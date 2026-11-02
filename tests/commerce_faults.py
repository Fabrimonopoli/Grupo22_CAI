from pathlib import Path
from decimal import Decimal
from datetime import datetime
import socket,tempfile,sys
import os,json,subprocess,urllib.request,urllib.error,threading,time,sqlite3,uuid,shutil,traceback
from concurrent.futures import ThreadPoolExecutor
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
ROOT=Path(__file__).resolve().parents[1]
REPO=ROOT
DATA=Path(tempfile.mkdtemp(prefix='ecommerce-faults-'))
ports=[]
for _ in range(4):
    with socket.socket() as port_socket:
        port_socket.bind(('127.0.0.1',0));ports.append(port_socket.getsockname()[1])
P,O,U,X=ports
USER=str(uuid.uuid4())
PROCS={};logs=[];checks=[];BLOCK_RELEASE=False;CRASH_RESERVE=False;reserved=threading.Event();proceed=threading.Event();failure=None
def req(port,method,path,body=None,headers=None):
    headers=dict(headers or {})
    if body is not None:headers['Content-Type']='application/json'
    data=(body.encode() if isinstance(body,str) else json.dumps(body).encode()) if body is not None else None
    r=urllib.request.Request(f'http://127.0.0.1:{port}{path}',data=data,headers=headers,method=method)
    try:r=urllib.request.urlopen(r,timeout=15)
    except urllib.error.HTTPError as e:r=e
    raw=r.read();
    try:b=json.loads(raw,parse_float=Decimal)
    except ValueError:b=raw.decode(errors='replace')
    return r.code,b
def check(name,value):
    checks.append({'name':name,'ok':bool(value)});print(name,value,flush=True)
    assert value,name
def db(query,args=()):
    with sqlite3.connect(DATA/'orders.db') as c:return c.execute(query,args).fetchall()
def launch(service,port):
    env=os.environ.copy();env.update({'ASPNETCORE_ENVIRONMENT':'Development','ASPNETCORE_URLS':f'http://127.0.0.1:{port}',
        'ConnectionStrings__DefaultConnection':f'Data Source={DATA/(service.lower()+".db")};Foreign Keys=True;',
        'Services__UsersApiUrl':f'http://127.0.0.1:{U}','Services__ProductsApiUrl':f'http://127.0.0.1:{X}', 'Services__OrdersApiUrl':f'http://127.0.0.1:{O}'})
    log=open(DATA/f'{service}.log','a',encoding='utf-8');logs.append(log)
    snapshot=DATA/(service+'-runtime')
    if not snapshot.exists():
        projects=REPO/'src' if (REPO/'src').exists() else REPO
        shutil.copytree(projects/f'{service}.API/bin/Debug/net10.0',snapshot)
    dll=snapshot/f'{service}.API.dll'
    PROCS[service]=subprocess.Popen([shutil.which('dotnet') or 'dotnet',str(dll)],cwd=DATA,env=env,stdout=log,stderr=subprocess.STDOUT,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    for _ in range(200):
        if PROCS[service].poll() is not None:raise Exception(f'{service} died; {DATA}')
        try:
            if req(port,'GET','/health/live')[0]==200:return
        except OSError:pass
        time.sleep(.1)
    raise Exception('startup timeout '+service)
def stop(service):
    p=PROCS.pop(service,None)
    if p:p.terminate();p.wait(timeout=10)
def product(stock=10):
    s,b=req(P,'POST','/api/products',{'nombre':'Probe '+uuid.uuid4().hex,'categoria':'Otros','descripcion':'Prueba','precio':125.5,'stock':stock})
    check('create product',s==201);return b['id']
def stock(pid):return req(P,'GET',f'/api/products/{pid}')[1]['stock']
def order(items,key=None):return req(O,'POST','/api/orders',{'usuarioId':USER,'items':[{'productoId':p,'cantidad':q} for p,q in items]}, {'Idempotency-Key':key} if key else None)
class Users(BaseHTTPRequestHandler):
    def do_GET(self):
        b={'id':USER,'nombre':'Prueba','apellido':'Probe','email':'probe@example.test','activo':True} if self.path.startswith('/api/users/') else {'status':'Healthy'}
        raw=json.dumps(b).encode();self.send_response(200);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(raw)));self.end_headers();self.wfile.write(raw)
    def log_message(self,*args):pass
class Proxy(BaseHTTPRequestHandler):
    protocol_version='HTTP/1.1'
    def send(self):
        global CRASH_RESERVE
        raw=b''
        if self.headers.get('Transfer-Encoding','').lower()=='chunked':
            while True:
                size=int(self.rfile.readline().strip().split(b';')[0],16)
                if size==0:self.rfile.readline();break
                raw+=self.rfile.read(size);self.rfile.read(2)
        else:raw=self.rfile.read(int(self.headers.get('Content-Length',0)))
        if self.command=='DELETE' and BLOCK_RELEASE:
            self.send_response(500);self.send_header('Content-Length','0');self.end_headers();return
        r=urllib.request.Request(f'http://127.0.0.1:{P}{self.path}',data=raw if raw else None,headers={'Content-Type':'application/json'},method=self.command)
        try:r=urllib.request.urlopen(r,timeout=12)
        except urllib.error.HTTPError as e:r=e
        body=r.read()
        if self.command=='POST' and CRASH_RESERVE:
            CRASH_RESERVE=False;reserved.set();proceed.wait(timeout=15)
        self.send_response(r.code);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(body)));self.end_headers()
        try:self.wfile.write(body)
        except (BrokenPipeError,ConnectionAbortedError):pass
    do_POST=send;do_PUT=send;do_DELETE=send;do_GET=send
    def log_message(self,*args):pass
class QuietServer(ThreadingHTTPServer):
    request_queue_size=128
    def handle_error(self,request,client_address):
        error=sys.exc_info()[1]
        if not isinstance(error,(ConnectionResetError,ConnectionAbortedError,BrokenPipeError)):super().handle_error(request,client_address)
ThreadingHTTPServer=QuietServer
us=ThreadingHTTPServer(('127.0.0.1',U),Users);proxy=ThreadingHTTPServer(('127.0.0.1',X),Proxy)
threading.Thread(target=us.serve_forever,daemon=True).start();threading.Thread(target=proxy.serve_forever,daemon=True).start()
try:
    launch('Products',P);launch('Orders',O)
    numeric='0.123456789012345678901234567'
    status,precise=req(P,'POST','/api/products','{"nombre":"Precisión '+uuid.uuid4().hex+'","categoria":"Otros","precio":'+numeric+',"stock":10}')
    check('exact decimal product survives persistence',status==201 and req(P,'GET','/api/products/'+precise['id'])[1]['precio']==Decimal(numeric))
    status,small=req(P,'POST','/api/products',{'nombre':'Precio pequeño '+uuid.uuid4().hex,'categoria':'Otros','precio':0.001,'stock':10})
    check('all positive decimal prices allowed by TP',status==201)
    status,priced_order=order([(precise['id'],3)])
    check('order captures exact decimal total',status==201 and priced_order['total']==Decimal(numeric)*3)
    intent_date=db('SELECT FechaCreacion FROM OrderCreationIntent WHERE OrderId=?',(priced_order['id'],))[0][0]
    check('order date preserves original UTC instant',abs((datetime.fromisoformat(priced_order['fechaCreacion'])-datetime.fromisoformat(intent_date)).total_seconds())<0.000001)
    p=product(5)
    with ThreadPoolExecutor(max_workers=10) as pool:responses=list(pool.map(lambda _:order([(p,1)]),range(10)))
    check('concurrent purchases conserve stock',sum(s==201 for s,b in responses)==5 and sum(s==422 for s,b in responses)==5 and stock(p)==0)
    p=product(10);s,b=order([(p,6),(p,6)])
    check('duplicate quantities aggregate against stock',s==422 and b['errorCode']=='ORD-005' and stock(p)==10)
    s,b=order([(p,2),(p,3)])
    check('valid duplicates saved as one item',s==201 and len(b['items'])==1 and b['items'][0]['cantidad']==5 and b['total']==627.5 and stock(p)==5)
    q=product(10);key=str(uuid.uuid4())
    with ThreadPoolExecutor(max_workers=4) as pool:rr=list(pool.map(lambda _:order([(q,2)],key),range(4)))
    check('parallel idempotent retry spends once',all(s==201 for s,b in rr) and len(set(b['id'] for s,b in rr))==1 and stock(q)==8)
    s,b=order([(q,3)],key);check('conflicting idempotency payload rejected',s==409 and b['errorCode']=='ORD-008')
    a,c=product(5),product(5);last=max([a,c]);key=str(uuid.uuid4())
    db(f"CREATE TRIGGER ProbeFail BEFORE INSERT ON OrderItem WHEN NEW.ProductoId='{last}' BEGIN SELECT RAISE(ABORT,'forced persistence failure'); END;")
    BLOCK_RELEASE=True
    s,b=order([(a,1),(c,1)],key)
    check('failed local transaction leaves no partial order',s==500 and db('SELECT COUNT(*) FROM "Order" WHERE Id=?',(key,))[0][0]==0 and db('SELECT COUNT(*) FROM OrderItem WHERE OrderId=?',(key,))[0][0]==0)
    check('compensation persists when Products unavailable',db('SELECT Action FROM OrderStockAction WHERE OrderId=?',(key,))==[('release',)] and stock(a)==4 and stock(c)==4)
    stop('Orders');BLOCK_RELEASE=False;db('DROP TRIGGER ProbeFail');launch('Orders',O)
    for _ in range(80):
        if stock(a)==5 and stock(c)==5:break
        time.sleep(.1)
    check('compensation resumes after restart',stock(a)==5 and stock(c)==5 and not db('SELECT * FROM OrderStockAction WHERE OrderId=?',(key,)))
    p=product(5);key=str(uuid.uuid4());CRASH_RESERVE=True
    with ThreadPoolExecutor(max_workers=1) as pool:
        task=pool.submit(order,[(p,2)],key)
        check('reserve reached crash point',reserved.wait(timeout=10));stop('Orders');proceed.set()
        try:task.result(timeout=10)
        except Exception:pass
    check('reservation durable across Orders crash',stock(p)==3 and db('SELECT Estado FROM OrderCreationIntent WHERE OrderId=?',(key,))==[('Processing',)])
    launch('Orders',O)
    for _ in range(100):
        s,b=req(O,'GET',f'/api/orders/{key}')
        if s==200:break
        time.sleep(.1)
    check('creation resumes after restart without double debit',s==200 and stock(p)==3 and b['items'][0]['cantidad']==2)
    s,b=req(P,'DELETE',f'/api/products/{p}');check('active order prevents product deletion',s==409)
    s,b=req(O,'PUT',f'/api/orders/{key}/status',{'estado':'Entregada'});check('invalid transition rejected',s==409 and b['errorCode']=='ORD-006')
    s,current=req(P,'GET',f'/api/products/{p}')
    oversized={k:current[k] for k in ['nombre','categoria','descripcion']};oversized.update(precio=125.5,stock=2147483647)
    status,error=req(P,'PUT',f'/api/products/{p}',oversized)
    check('catalog update preserves capacity for compensation',status==400 and error['errorCode']=='PRD-002' and stock(p)==3)
    s,b=req(O,'PUT',f'/api/orders/{key}/status',{'estado':'Cancelada'});check('cancel restores reservation once',s==200 and stock(p)==5)
    s,b=req(O,'PUT',f'/api/orders/{key}/status',{'estado':'Cancelada'});check('repeated cancel is idempotent',s==200 and stock(p)==5)
    p=product(5);key=str(uuid.uuid4());req(P,'DELETE',f'/internal/stock/reservations/{key}')
    s,b=req(P,'POST',f'/internal/stock/reservations/{key}',{'items':[{'productoId':p,'cantidad':2}]})
    check('release tombstone rejects late reserve',s==409 and stock(p)==5)
except Exception:
    failure=traceback.format_exc();print(failure,flush=True)
finally:
    for service in list(PROCS):stop(service)
    us.shutdown();proxy.shutdown()
    for log in logs:log.close()
    (DATA/'results.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
    print('RESULTS',DATA,len(checks),'checks',sum(c['ok'] for c in checks),'passed',flush=True)

if failure or not all(c['ok'] for c in checks):sys.exit(1)
