"""Regresión HTTP con procesos y bases descartables; no toca los .db del repositorio."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import argparse, json, os, shutil, socket, sqlite3, subprocess, tempfile, time, urllib.error, urllib.request, uuid

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'src' if (ROOT / 'src').exists() else ROOT
PORTS = {'Products':26110, 'Users':26112, 'Orders':26114, 'Cart':26138, 'Notifications':26118}
processes, handles, checks = {}, [], []
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=Path, default=ROOT / '.test-runs/latest.json')
parser.add_argument('--keep-data', action='store_true', help='Conservar las bases y logs sintéticos para diagnóstico.')
args = parser.parse_args()
ROOT.joinpath('.test-runs').mkdir(exist_ok=True)
DATA = Path(tempfile.mkdtemp(prefix='http-', dir=ROOT / '.test-runs'))
DOTNET = shutil.which('dotnet') or 'dotnet'

def check(label, ok, detail=None):
    checks.append({'name':label, 'passed':bool(ok), 'detail':detail})
    print(('PASS ' if ok else 'FAIL ') + label, flush=True)

def req(service, method, path, body=None, headers=None, raw=None):
    headers = dict(headers or {})
    if body is not None: raw = json.dumps(body).encode()
    if raw is not None: headers.setdefault('Content-Type','application/json')
    request = urllib.request.Request(f'http://127.0.0.1:{PORTS[service]}{path}', data=raw, headers=headers, method=method)
    try: response = urllib.request.urlopen(request,timeout=20)
    except urllib.error.HTTPError as ex: response = ex
    text = response.read().decode('utf-8',errors='replace')
    try: value = json.loads(text)
    except ValueError: value = text
    return {'status':response.code,'headers':dict(response.headers),'body':value}

def expect(label, service, method, path, status, body=None, code=None, **kwargs):
    response = req(service, method, path, body, **kwargs)
    check(label, response['status']==status, {'expected':status, 'response':response})
    if code:
        data=response['body']
        required={'type','title','status','detail','instance','errorCode','errorMessage','correlationId'}
        check(label+' contrato',isinstance(data,dict) and required<=data.keys()
              and data.get('errorCode')==code and data.get('status')==status
              and bool(data.get('errorMessage')) and data.get('correlationId')==response['headers'].get('X-Correlation-Id'),response)
    return response['body']

def start(service, environment='Development'):
    with socket.socket() as sock:sock.bind(('127.0.0.1',PORTS[service]))
    env=os.environ.copy()
    env.update({'ASPNETCORE_URLS':f'http://127.0.0.1:{PORTS[service]}','ASPNETCORE_ENVIRONMENT':environment,
        'ConnectionStrings__DefaultConnection':f'Data Source={DATA/(service.lower()+".db")};Foreign Keys=True;',
        'Observability__LogPath':str(DATA/f'{service.lower()}-.jsonl'), 'Observability__MinimumLevel':'Information'})
    for dependency in ['Products','Users','Orders']:env[f'Services__{dependency}ApiUrl']=f'http://127.0.0.1:{PORTS[dependency]}'
    handle=open(DATA/f'{service.lower()}.stdout.log','w',encoding='utf-8');handles.append(handle)
    dll=SRC/f'{service}.API/bin/Debug/net10.0/{service}.API.dll'
    kwargs={'creationflags':subprocess.CREATE_NO_WINDOW} if os.name=='nt' else {}
    processes[service]=subprocess.Popen([DOTNET,str(dll)],cwd=SRC/f'{service}.API',env=env,stdout=handle,stderr=subprocess.STDOUT,**kwargs)
    for _ in range(150):
        if processes[service].poll() is not None:raise RuntimeError(f'{service} no arrancó; revisar {handle.name}')
        try:
            if req(service,'GET','/health/live')['status']==200:return
        except (OSError,urllib.error.URLError):pass
        time.sleep(.1)
    raise TimeoutError(service)

def stop(service):
    process=processes.pop(service,None)
    if process:
        process.terminate()
        try:process.wait(timeout=10)
        except subprocess.TimeoutExpired:process.kill();process.wait(timeout=5)

def sql(service, query, params=()):
    with sqlite3.connect(DATA/(service.lower()+'.db')) as conn:return conn.execute(query,params).fetchall()

def product(name, stock=10, **kwargs):
    return {'nombre':name,'descripcion':'Regresión automática','precio':125.50,'stock':stock,'categoria':'Otros',**kwargs}

def order(uid,pid,quantity=1):return {'usuarioId':uid,'items':[{'productoId':pid,'cantidad':quantity}]}

try:
    for service in PORTS:start(service)
    for service in PORTS:
        for path in ['/health','/health/ready','/health/live']:
            health=expect(service+' '+path,service,'GET',path,200)
            check(service+' estado JSON '+path,isinstance(health,dict) and health.get('status')=='Healthy',health)
        expect(service+' Swagger UI',service,'GET','/swagger/index.html',200)
        document=expect(service+' OpenAPI',service,'GET','/swagger/v1/swagger.json',200)
        operations=[op for entry in document['paths'].values() for method,op in entry.items() if method.lower() in ['get','post','put','delete']]
        check(service+' responses documentados',all(all('content' in r or status=='204' for status,r in op['responses'].items()) for op in operations),document['paths'])
        check(service+' ejemplos de error',all('500' in op['responses'] and 'examples' in op['responses']['500']['content']['application/problem+json'] for op in operations),document['paths'])
        if service=='Cart':check('Cart documenta errores de GUID',all('400' in op['responses'] for op in operations))
        if service=='Orders':check('Orders documenta query inválido','400' in document['paths']['/api/orders']['get']['responses'])
        expect(service+' ruta inexistente',service,'GET','/ruta-inexistente',404,code='HTTP-404')

    register={'nombre':'Prueba','apellido':'Regresión','email':'audit@example.test','password':'Prueba123!'}
    uid=expect('registro válido','Users','POST','/api/users/register',201,register)['id']
    expect('registro email inválido','Users','POST','/api/users/register',400,{**register,'email':'invalido'},'USR-002')
    expect('registro duplicado normalizado','Users','POST','/api/users/register',409,{**register,'email':'AUDIT@example.test'},'USR-001')
    login={'email':register['email'],'password':register['password']}
    response=expect('login correcto','Users','POST','/api/users/login',200,login)
    check('sin PasswordHash en respuesta','passwordHash' not in json.dumps(response))
    expect('login erróneo','Users','POST','/api/users/login',401,{**login,'password':'x'},'USR-003')
    expect('login resetea contador','Users','POST','/api/users/login',200,login)
    check('contador reseteado en base',sql('Users','SELECT IntentosFallidos FROM User WHERE Id=?',(uid,))==[(0,)])
    locked=expect('crear usuario de bloqueo','Users','POST','/api/users/register',201,{**register,'email':'locked@example.test'})['id']
    for number in range(1,4):
        expect('fallo consecutivo '+str(number),'Users','POST','/api/users/login',401 if number<3 else 403,
            {'email':'locked@example.test','password':'x'},'USR-003' if number<3 else 'USR-004')
    expect('cuenta bloqueada rechaza clave correcta','Users','POST','/api/users/login',403,{'email':'locked@example.test','password':'Prueba123!'},'USR-004')
    fraud=expect('crear usuario fraude','Users','POST','/api/users/register',201,{**register,'email':'fraud@example.test'})['id']
    sql('Users','UPDATE User SET Activo=0 WHERE Id=?',(fraud,))
    expect('bloqueo manual','Users','POST','/api/users/login',403,{'email':'fraud@example.test','password':'Prueba123!'},'USR-005')
    expect('usuario interno ausente','Users','GET',f'/api/users/{uuid.uuid4()}',404,code='USR-007')
    stored=sql('Users','SELECT PasswordHash FROM User WHERE Id=?',(uid,))[0][0]
    check('contraseña salteada no es Base64 de la clave',stored.startswith('$identity$') and stored!='UHJ1ZWJhMTIzIQ==')

    pid=expect('crear producto','Products','POST','/api/products',201,product('Principal',20))['id']
    expect('producto ausente','Products','GET',f'/api/products/{uuid.uuid4()}',404,code='PRD-001')
    expect('nombre máximo','Products','POST','/api/products',400,product('n'*101),'PRD-002')
    expect('descripción máxima','Products','POST','/api/products',400,product('Largo',descripcion='x'*501),'PRD-002')
    expect('precio inválido','Products','POST','/api/products',400,product('Precio',precio=0),'PRD-002')
    expect('stock inválido','Products','POST','/api/products',400,product('Stock',stock=-1),'PRD-002')
    missing_stock=product('Sin stock');missing_stock.pop('stock')
    expect('stock obligatorio','Products','POST','/api/products',400,missing_stock,'PRD-002')
    expect('producto duplicado','Products','POST','/api/products',409,product('Principal',20),'PRD-003')
    listed=expect('filtros combinados','Products','GET','/api/products?categoria=otros&nombre=principal',200)
    check('filtros encuentran el producto',len(listed)==1 and listed[0]['id']==pid)
    expect('categoría libre autorizada','Products','POST','/api/products',201,product('Categoría',categoria='Libre'))

    expect('Orders query GUID inválido','Orders','GET','/api/orders?usuarioId=invalid',400,code='ORD-002')
    expect('orden vacía','Orders','POST','/api/orders',400,{'usuarioId':uid,'items':[]},'ORD-002')
    expect('orden sin UsuarioId','Orders','POST','/api/orders',400,{'items':[{'productoId':pid,'cantidad':1}]},'ORD-002')
    expect('orden sin ProductoId','Orders','POST','/api/orders',400,{'usuarioId':uid,'items':[{'cantidad':1}]},'ORD-002')
    expect('orden ítem nulo','Orders','POST','/api/orders',400,{'usuarioId':uid,'items':[None]},'ORD-002')
    expect('orden usuario ausente','Orders','POST','/api/orders',404,order(str(uuid.uuid4()),pid),'ORD-003')
    expect('orden producto ausente','Orders','POST','/api/orders',404,order(uid,str(uuid.uuid4())),'ORD-004')
    expect('orden stock insuficiente','Orders','POST','/api/orders',422,order(uid,pid,21),'ORD-005')
    good=expect('orden válida','Orders','POST','/api/orders',201,order(uid,pid,2));oid=good['id']
    check('total y precio capturados',good['total']==251 and good['items'][0]['precioUnitario']==125.5,good)
    current=expect('stock descontado','Products','GET',f'/api/products/{pid}',200)
    check('stock posterior 18',current['stock']==18,current)
    expect('borrado con orden activa','Products','DELETE',f'/api/products/{pid}',409,code='PRD-004')
    expect('confirmar orden','Orders','PUT',f'/api/orders/{oid}/status',200,{'estado':'Confirmada'})
    expect('retroceso de estado rechazado','Orders','PUT',f'/api/orders/{oid}/status',409,{'estado':'Pendiente'},'ORD-006')
    expect('enviar orden','Orders','PUT',f'/api/orders/{oid}/status',200,{'estado':'Enviada'})
    expect('entregar orden','Orders','PUT',f'/api/orders/{oid}/status',200,{'estado':'Entregada'})
    expect('estado terminal rechazado','Orders','PUT',f'/api/orders/{oid}/status',409,{'estado':'Pendiente'},'ORD-006')
    expect('orden ausente','Orders','GET',f'/api/orders/{uuid.uuid4()}',404,code='ORD-001')
    repeated=expect('crear producto para repetidos','Products','POST','/api/products',201,product('Repetidos'))['id']
    before=sql('Orders','SELECT COUNT(*) FROM "Order"')[0][0]
    expect('repetidos validan cantidad agregada','Orders','POST','/api/orders',422,{'usuarioId':uid,'items':[{'productoId':repeated,'cantidad':6},{'productoId':repeated,'cantidad':6}]},'ORD-005')
    check('repetidos rechazados sin orden parcial',sql('Orders','SELECT COUNT(*) FROM "Order"')[0][0]==before)
    check('repetidos rechazados sin descuento',req('Products','GET',f'/api/products/{repeated}')['body']['stock']==10)

    concurrent=expect('producto de concurrencia','Products','POST','/api/products',201,product('Concurrencia',5))['id']
    with ThreadPoolExecutor(max_workers=10) as pool: responses=list(pool.map(lambda _:req('Orders','POST','/api/orders',order(uid,concurrent)),range(10)))
    accepted=sum(r['status']==201 for r in responses)
    remaining=req('Products','GET',f'/api/products/{concurrent}')['body']['stock']
    check('sin sobreventa concurrente',accepted==5 and remaining==0 and all(r['status'] in [201,422] for r in responses),{'accepted':accepted,'remaining':remaining,'statuses':[r['status'] for r in responses]})
    key=str(uuid.uuid4());payload=order(uid,repeated)
    first=expect('orden idempotente','Orders','POST','/api/orders',201,payload,headers={'Idempotency-Key':key})
    second=expect('reintento idempotente','Orders','POST','/api/orders',201,payload,headers={'Idempotency-Key':key})
    check('reintento no duplica orden ni stock',first['id']==second['id'] and req('Products','GET',f'/api/products/{repeated}')['body']['stock']==9)
    expect('clave reutilizada con otro payload','Orders','POST','/api/orders',409,order(uid,repeated,2),'ORD-008',headers={'Idempotency-Key':key})

    cp=expect('producto carrito','Products','POST','/api/products',201,product('Carrito'))['id']
    expect('Cart ruta GUID inválido','Cart','GET','/api/cart/invalid',400,code='CRT-004')
    expect('carrito ausente','Cart','GET',f'/api/cart/{uid}',404,code='CRT-001')
    expect('carrito cantidad inválida','Cart','POST',f'/api/cart/{uid}/items',400,{'productoId':cp,'cantidad':0},'CRT-004')
    expect('carrito producto ausente','Cart','POST',f'/api/cart/{uid}/items',404,{'productoId':str(uuid.uuid4()),'cantidad':1},'CRT-002')
    expect('carrito agregar seis','Cart','POST',f'/api/cart/{uid}/items',200,{'productoId':cp,'cantidad':6})
    expect('carrito suma excesiva','Cart','POST',f'/api/cart/{uid}/items',422,{'productoId':cp,'cantidad':6},'CRT-003')
    a=expect('fecha carrito lectura 1','Cart','GET',f'/api/cart/{uid}',200)
    b=expect('fecha carrito lectura 2','Cart','GET',f'/api/cart/{uid}',200)
    check('GET conserva fecha de última modificación',a['fechaActualizacion']==b['fechaActualizacion'])
    expect('carrito actualizar','Cart','PUT',f'/api/cart/{uid}/items/{cp}',200,{'cantidad':4})
    expect('carrito quitar','Cart','DELETE',f'/api/cart/{uid}/items/{cp}',204)
    expect('carrito volver a agregar','Cart','POST',f'/api/cart/{uid}/items',200,{'productoId':cp,'cantidad':1})
    expect('carrito vaciar','Cart','DELETE',f'/api/cart/{uid}',204)
    expect('carrito vacío ausente','Cart','GET',f'/api/cart/{uid}',404,code='CRT-001')

    expect('notificaciones sin historial','Notifications','GET',f'/api/notifications/{uid}',404,code='NTF-003')
    expect('notificación tipo inválido','Notifications','POST','/api/notifications/send',400,{'usuarioId':uid,'tipo':'Cualquiera','mensaje':'Prueba'},'NTF-002')
    expect('notificación usuario ausente','Notifications','POST','/api/notifications/send',404,{'usuarioId':str(uuid.uuid4()),'tipo':'Email','mensaje':'Prueba'},'NTF-001')
    expect('notificación larga','Notifications','POST','/api/notifications/send',400,{'usuarioId':uid,'tipo':'Email','mensaje':'x'*501},'NTF-002')
    sent=expect('notificación válida','Notifications','POST','/api/notifications/send',201,{'usuarioId':uid,'tipo':'Email','mensaje':'Prueba'})
    check('notificación contrato completo',sent['estado']=='Enviada' and bool(sent['fechaEnvio']) and 'fechaCreacion' not in sent,sent)
    history=expect('leer notificaciones pobladas','Notifications','GET',f'/api/notifications/{uid}',200)
    check('historial contiene envío',len(history)==1 and history[0]['id']==sent['id'])
    for service,path,code in [('Products','/api/products','PRD-002'),('Users','/api/users/register','USR-002'),('Orders','/api/orders','ORD-002'),('Cart',f'/api/cart/{uid}/items','CRT-004'),('Notifications','/api/notifications/send','NTF-002')]:
        expect(service+' JSON inválido',service,'POST',path,400,code=code,raw=b'{',headers={'X-Correlation-Id':'regression-correlation-123'})
    stop('Orders');start('Orders')
    expect('orden persiste tras reiniciar','Orders','GET',f'/api/orders/{oid}',200)
    stop('Notifications');start('Notifications')
    expect('notificación persiste tras reiniciar','Notifications','GET',f'/api/notifications/{uid}',200)
    stop('Users')
    expect('dependencia caída usa NTF004','Notifications','POST','/api/notifications/send',500,{'usuarioId':uid,'tipo':'Email','mensaje':'Prueba'},'NTF-004')
    expect('readiness detecta dependencia caída','Notifications','GET','/health/ready',503)
    expect('liveness permanece sano','Notifications','GET','/health/live',200)
    start('Users','Production')
    expect('binding consistente en Production','Users','POST','/api/users/register',400,code='USR-002',raw=b'{')
    expect('Swagger disponible en Production','Users','GET','/swagger/index.html',200)
    sql('Users','DROP TABLE User')
    expect('health detecta tabla ausente','Users','GET','/health/local',503)
    expect('readiness detecta almacenamiento remoto caído','Orders','GET','/health/ready',503)
    expect('proceso sigue vivo sin almacenamiento','Users','GET','/health/live',200)
    records=[]
    for path in DATA.glob('*.jsonl'):
        records.extend(json.loads(line) for line in path.read_text(encoding='utf-8').splitlines() if line.strip())
    check('logs JSON estructurados',bool(records) and all({'Timestamp','Level','Properties'}<=r.keys() and 'Servicio' in r['Properties'] for r in records))
    warnings=[r for r in records if r['Level']=='Warning' and r['Properties'].get('ErrorCode')]
    check('errores de negocio Warning con correlación',bool(warnings) and all('CorrelationId' in r['Properties'] for r in warnings))
    errors=[r for r in records if r['Level']=='Error' and r['Properties'].get('ErrorCode')=='NTF-004']
    check('errores inesperados Error con código',bool(errors))
except Exception as ex:
    import traceback
    check('ejecución completa',False,traceback.format_exc())
finally:
    for service in list(processes):stop(service)
    for handle in handles:handle.close()
    summary={'checks':len(checks),'passed':sum(c['passed'] for c in checks),'failed':sum(not c['passed'] for c in checks),'dataDirectory':str(DATA),'results':checks}
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(summary,indent=2,ensure_ascii=False),encoding='utf-8')
    print(json.dumps({k:v for k,v in summary.items() if k!='results'},ensure_ascii=False),flush=True)
    if not args.keep_data and not summary['failed']:shutil.rmtree(DATA)
    raise SystemExit(1 if summary['failed'] else 0)
