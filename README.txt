Tickets Andretich - MVP local
=============================

Requisitos
- .NET SDK 8
- Docker Desktop (o SQL Server local)

Levantar SQL Server con Docker
------------------------------
    docker compose up -d

Configurar base de datos y correr
---------------------------------
    cd TicketsAndretich.Web
    dotnet restore
    dotnet tool install --global dotnet-ef || true
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    dotnet run

Login
-----
Admin inicial:
  usuario: admin@andretich.local
  clave:   Admin123$

Emails de prueba
----------------
Se guardan como .txt en App_Data/emails

Adjuntos
--------
Se guardan en App_Data/uploads

Notas
-----
- Es una versión mínima de prueba. No incluye todavía wizard de setup ni OAuth2.
- Los botones son rojos (#C62828) y el fondo es blanco, como se pidió.
- Dashboard simple con contadores y promedio de minutos de resolución.
