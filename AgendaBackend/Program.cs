// Importamos el proveedor oficial de SQLite para ADO.NET
using Microsoft.Data.Sqlite;

// Configuramos el host web con el patrón Minimal APIs de ASP.NET Core
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Cadena de conexión: creará un archivo local llamado "database.db"
string connectionString = "Data Source=database.db";

// Garantizamos que la tabla exista antes de procesar cualquier petición HTTP.
// 'using' asegura el cierre y liberación inmediata de los recursos nativos de SQLite.
using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();

    // DDL: Id autoincremental como clave primaria y campos de texto para la entidad
    string sql = "CREATE TABLE IF NOT EXISTS Contactos (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nombre TEXT, Telefono TEXT)";

    using var command = new SqliteCommand(sql, connection);
    command.ExecuteNonQuery(); // Ejecución sin retorno de filas
}

// CRUD: Create, Read, Update, Delete

// --- GET: Listar todos los contactos ---
app.MapGet("/contactos", () =>
{
    var lista = new List<object>();

    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = new SqliteCommand("SELECT * FROM Contactos", connection);
    using var reader = command.ExecuteReader();

    // Iteramos secuencialmente por el cursor de resultados
    while (reader.Read())
    {
        lista.Add(new
        {
            Id = reader["Id"],
            Nombre = reader["Nombre"],
            Telefono = reader["Telefono"]
        });
    }

    // 200 OK con el array serializado a JSON
    return Results.Ok(lista);
});

// --- POST: Crear un nuevo contacto ---
// Minimal APIs deserializa el body JSON entrante directamente en el record 'Contacto'
app.MapPost("/contactos", (Contacto nuevo) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    string sql = "INSERT INTO Contactos (Nombre, Telefono) VALUES (@Nombre, @Telefono)";
    using var command = new SqliteCommand(sql, connection);

    // Parámetros SQL: evitan inyección de código malicioso (SQL Injection)
    command.Parameters.AddWithValue("@Nombre", nuevo.Nombre);
    command.Parameters.AddWithValue("@Telefono", nuevo.Telefono);
    command.ExecuteNonQuery();

    // 201 Created con cabecera de ubicación y el recurso enviado
    return Results.Created($"/contactos/{nuevo.Nombre}", nuevo);
});

// --- DELETE: Eliminar contacto por ID ---
// El parámetro 'id' se extrae automáticamente del segmento de la URL
app.MapDelete("/contactos/{id}", (int id) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    using var command = new SqliteCommand("DELETE FROM Contactos WHERE Id = @Id", connection);
    command.Parameters.AddWithValue("@Id", id);

    // ExecuteNonQuery retorna el número de registros modificados/eliminados
    int filas = command.ExecuteNonQuery();

    // Validamos: si no afectó registros, el recurso no existía
    return filas > 0 ? Results.Ok("Contacto eliminado") : Results.NotFound();
});

// --- PUT: Actualizar contacto existente por ID ---
app.MapPut("/contactos/{id}", (int id, Contacto actualizado) =>
{
    using var connection = new SqliteConnection(connectionString);
    connection.Open();

    string sql = "UPDATE Contactos SET Nombre = @Nombre, Telefono = @Telefono WHERE Id = @Id";
    using var command = new SqliteCommand(sql, connection);

    command.Parameters.AddWithValue("@Nombre", actualizado.Nombre);
    command.Parameters.AddWithValue("@Telefono", actualizado.Telefono);
    command.Parameters.AddWithValue("@Id", id);

    int filas = command.ExecuteNonQuery();

    // 200 OK si se actualizó, 404 Not Found si el ID no coincidió con ninguna fila
    return filas > 0 ? Results.Ok("Contacto actualizado") : Results.NotFound();
});

// Arranca el servidor web y queda a la escucha de peticiones HTTP
app.Run();

// MODELO DE DATOS (DTO)

// 'record': estructura inmutable y concisa introducida en C# 9 ideal para transferencia de datos
record Contacto(string Nombre, string Telefono);