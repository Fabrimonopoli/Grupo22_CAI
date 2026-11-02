using System.Text;
using Cart.API.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Users.API.DTOs;
using Users.API.Services;
using UserRepository = Users.API.Data.UserRepository;
using UserInitializer = Users.API.Data.DatabaseInitializer;

// Ejecutable sin paquetes de test adicionales. Usa exclusivamente bases temporales propias.
var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
var directory = Path.GetFullPath(Path.Combine(tempRoot, "ecommerce-users-cart-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(directory);
var checks = 0;
void Check(bool success, string name)
{
    if (!success)
        throw new InvalidOperationException("FALLÓ: " + name);
    Console.WriteLine("OK " + name);
    checks++;
}

IConfiguration Config(string name) => new ConfigurationBuilder().AddInMemoryCollection(
    new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] =
        $"Data Source={Path.Combine(directory, name + ".db")};Foreign Keys=True;Pooling=False;"
    }).Build();

try
{
    SqlMapper.AddTypeHandler(new Users.API.Data.SqliteGuidTypeHandler());
    var userConfig = Config("users");
    var legacyId = Guid.NewGuid();
    using (var connection = new SqliteConnection(userConfig.GetConnectionString("DefaultConnection")))
    {
        connection.Open();
        connection.Execute("""
            CREATE TABLE User (Id TEXT PRIMARY KEY, Nombre TEXT NOT NULL, Apellido TEXT NOT NULL,
              Email TEXT NOT NULL UNIQUE, PasswordHash TEXT NOT NULL, FechaRegistro TEXT NOT NULL,
              Activo INTEGER NOT NULL, IntentosFallidos INTEGER NOT NULL);
            INSERT INTO User VALUES (@id,'Cuenta','Anterior','LEGACY@example.test',@hash,@date,1,0);
            """, new
        {
            id = legacyId.ToString(),
            hash = Convert.ToBase64String(Encoding.UTF8.GetBytes("LegacyPass!")),
            date = DateTime.UtcNow.ToString("O")
        });
    }
    var usersInitializer = new UserInitializer(userConfig, NullLogger<UserInitializer>.Instance);
    usersInitializer.Initialize();
    usersInitializer.Initialize();
    var users = new UserService(new UserRepository(userConfig));
    var legacy = await users.LoginAsync(new LoginUserRequest { Email = "legacy@example.test", Password = "LegacyPass!" });
    Check(legacy.Id == legacyId, "Migración conserva cuenta anterior y normaliza email");
    using (var connection = new SqliteConnection(userConfig.GetConnectionString("DefaultConnection")))
        Check((await connection.QuerySingleAsync<string>("SELECT PasswordHash FROM User WHERE Id=@id", new
        {
            id = legacyId.ToString()
        })).StartsWith("$identity$", StringComparison.Ordinal),
            "Login correcto reemplaza Base64 heredado por hash con salt");
    Check((await users.LoginAsync(new LoginUserRequest { Email = "legacy@example.test", Password = "LegacyPass!" })).Id == legacyId,
        "La contraseña migrada sigue funcionando");

    var registration = new RegisterUserRequest { Nombre = "Prueba", Apellido = "Automática", Email = "new@example.test", Password = "NewPass!" };
    var registered = await users.RegisterAsync(registration);
    Check(!System.Text.Json.JsonSerializer.Serialize(registered).Contains("Password", StringComparison.OrdinalIgnoreCase),
        "Respuesta de registro no expone contraseña");
    var duplicateRejected = false;
    try
    {
        await users.RegisterAsync(new RegisterUserRequest { Nombre = "Otro", Apellido = "Usuario", Email = "NEW@example.test", Password = "OtherPass!" });
    }
    catch (Users.API.Exceptions.BusinessRuleException ex) { duplicateRejected = ex.ErrorCode == "USR-001"; }
    Check(duplicateRejected, "Unicidad de email ignora mayúsculas");
    var emailRejected = false;
    try
    {
        await users.RegisterAsync(new RegisterUserRequest { Nombre = "Mal", Apellido = "Email", Email = "sin-email", Password = "Pass!" });
    }
    catch (Users.API.Exceptions.BusinessRuleException ex) { emailRejected = ex.ErrorCode == "USR-002"; }
    Check(emailRejected, "Email inválido produce USR-002");

    try
    {
        await users.LoginAsync(new LoginUserRequest { Email = registration.Email, Password = "incorrecta" });
    }
    catch (Users.API.Exceptions.BusinessRuleException ex) when (ex.ErrorCode == "USR-003") { }
    await users.LoginAsync(new LoginUserRequest { Email = registration.Email, Password = "NewPass!" });
    using (var connection = new SqliteConnection(userConfig.GetConnectionString("DefaultConnection")))
        Check(await connection.QuerySingleAsync<int>("SELECT IntentosFallidos FROM User WHERE Id=@id", new
        {
            id = registered.Id.ToString()
        }) == 0,
            "Login correcto resetea el contador después de un fallo");
    using var registrationGate = new ManualResetEventSlim(false);
    var concurrentRegistrations = Enumerable.Range(0, 8).Select(n => Task.Run(async () =>
    {
        registrationGate.Wait();
        try
        {
            await users.RegisterAsync(new RegisterUserRequest
            {
                Nombre = "Concurrente",
                Apellido = "Registro",
                Email = n % 2 == 0 ? "parallel@example.test" : "PARALLEL@example.test",
                Password = "ParallelPass!"
            });
            return true;
        }
        catch (Users.API.Exceptions.BusinessRuleException ex) when (ex.ErrorCode == "USR-001") { return false; }
    })).ToArray();
    registrationGate.Set();
    Check((await Task.WhenAll(concurrentRegistrations)).Count(x => x) == 1, "Registros simultáneos crean una sola cuenta por email normalizado");
    using var gate = new ManualResetEventSlim(false);
    var attempts = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
    {
        gate.Wait();
        try
        {
            await users.LoginAsync(new LoginUserRequest { Email = registration.Email, Password = "incorrecta" });
            return "SUCCESS";
        }
        catch (Users.API.Exceptions.BusinessRuleException ex) { return ex.ErrorCode; }
    })).ToArray();
    gate.Set();
    var errors = await Task.WhenAll(attempts);
    Check(errors.Count(x => x == "USR-003") == 2 && errors.Count(x => x == "USR-004") == 6,
        "Intentos simultáneos bloquean exactamente en el tercer fallo");
    using (var connection = new SqliteConnection(userConfig.GetConnectionString("DefaultConnection")))
    {
        var state = await connection.QuerySingleAsync<(int Activo, int IntentosFallidos)>(
            "SELECT Activo,IntentosFallidos FROM User WHERE Id=@id", new
            {
                id = registered.Id.ToString()
            });
        Check(state.Activo == 0 && state.IntentosFallidos == 3, "Bloqueo y contador permanecen consistentes");
        await connection.ExecuteAsync("UPDATE User SET Activo=0,IntentosFallidos=0 WHERE Id=@id", new
        {
            id = registered.Id.ToString()
        });
    }
    var fraudRejected = false;
    try
    {
        await users.LoginAsync(new LoginUserRequest { Email = registration.Email, Password = "NewPass!" });
    }
    catch (Users.API.Exceptions.BusinessRuleException ex) { fraudRejected = ex.ErrorCode == "USR-005"; }
    Check(fraudRejected, "Bloqueo manual conserva USR-005");

    var cartConfig = Config("cart");
    var cartUser = Guid.NewGuid();
    var cartProduct = Guid.NewGuid();
    using (var connection = new SqliteConnection(cartConfig.GetConnectionString("DefaultConnection")))
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            CREATE TABLE CartItems (UserId TEXT, ProductId TEXT, Cantidad INTEGER, PRIMARY KEY(UserId, ProductId));
            INSERT INTO CartItems VALUES (@userId,@productId,1);
            """, new
        {
            userId = cartUser.ToString(),
            productId = cartProduct.ToString()
        });
    }
    var cartInitializer = new Cart.API.Data.DatabaseInitializer(cartConfig);
    await cartInitializer.InitializeAsync();
    var cart = new CartRepository(cartConfig);
    var migrated = await cart.GetCartAsync(cartUser);
    Check(migrated.Items.Single().Cantidad == 1, "Migración conserva ítems existentes del carrito");
    var initialDate = migrated.FechaActualizacion;
    await cartInitializer.InitializeAsync();
    Check((await cart.GetCartAsync(cartUser)).FechaActualizacion == initialDate, "Reinicializar no modifica fecha histórica del carrito");
    await cart.ClearCartAsync(cartUser);
    using var cartGate = new ManualResetEventSlim(false);
    var adds = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
    {
        cartGate.Wait();
        try
        {
            await cart.AddItemAsync(cartUser, cartProduct, 4, 10);
            return true;
        }
        catch (Cart.API.Exceptions.BusinessRuleException ex) when (ex.Code == "CRT-003") { return false; }
    })).ToArray();
    cartGate.Set();
    var added = await Task.WhenAll(adds);
    var finalCart = await cart.GetCartAsync(cartUser);
    Check(added.Count(x => x) == 2 && finalCart.Items.Single().Cantidad == 8,
        "Agregados simultáneos respetan cantidad acumulada y stock");
    var secondRead = await cart.GetCartAsync(cartUser);
    Check(secondRead.FechaActualizacion == finalCart.FechaActualizacion, "GET sin escritura conserva FechaActualizacion");
    var updated = await cart.UpdateItemQuantityAsync(cartUser, cartProduct, 3, 10);
    Check(updated.Items.Single().Cantidad == 3 && updated.FechaActualizacion >= finalCart.FechaActualizacion,
        "PUT cambia cantidad y fecha en una misma transacción");
    await cart.RemoveItemAsync(cartUser, cartProduct);
    var emptyMissing = false;
    try
    {
        await cart.GetCartAsync(cartUser);
    }
    catch (Cart.API.Exceptions.NotFoundException ex) { emptyMissing = ex.Code == "CRT-001"; }
    Check(emptyMissing, "Carrito sin ítems se considera inactivo");
    Console.WriteLine($"{checks} comprobaciones aprobadas.");
}
finally
{
    SqliteConnection.ClearAllPools();
    if (Directory.GetParent(directory)?.FullName != tempRoot ||
        !Path.GetFileName(directory).StartsWith("ecommerce-users-cart-", StringComparison.Ordinal))
        throw new InvalidOperationException("El directorio de prueba no está dentro del temporal esperado.");
    Directory.Delete(directory, recursive: true);
}
