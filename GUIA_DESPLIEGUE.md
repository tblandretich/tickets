# Guía de Despliegue - Tickets Andretich

## 📋 Requisitos Previos

### Para IIS (Windows Server)
- **Windows Server 2016/2019/2022** o Windows 10/11 Pro
- **IIS** (Internet Information Services) habilitado
- **.NET 8.0 Hosting Bundle** - [Descargar aquí](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** 2019/2022 (Express, Standard o Enterprise)

### Para GitHub
- **Git** instalado - [Descargar aquí](https://git-scm.com/downloads)
- Cuenta de **GitHub**

---

## 🚀 Opción 1: Despliegue en IIS

### Paso 1: Instalar .NET 8.0 Hosting Bundle
1. Descargar de: https://dotnet.microsoft.com/download/dotnet/8.0
2. Buscar "Hosting Bundle" en la sección "ASP.NET Core Runtime"
3. Ejecutar el instalador como Administrador
4. **Reiniciar IIS** después de instalar:
```powershell
iisreset
```

### Paso 2: Habilitar IIS (si no está)
```powershell
# Ejecutar como Administrador en PowerShell
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45
```

### Paso 3: Copiar archivos publicados
1. Los archivos están en: `C:\Users\Lucas\Desktop\Ticket\Local\TicketsAndretich.Web\publish\`
2. Copiar **toda la carpeta `publish`** a tu servidor
3. Renombrar a algo como `TicketsAndretich` y mover a `C:\inetpub\wwwroot\TicketsAndretich`

### Paso 4: Crear sitio en IIS
1. Abrir **Administrador de IIS** (inetmgr)
2. Click derecho en "Sitios" → "Agregar sitio web"
3. Configurar:
   - **Nombre del sitio**: TicketsAndretich
   - **Ruta física**: `C:\inetpub\wwwroot\TicketsAndretich`
   - **Puerto**: 80 (o el que prefieras)
   - **Nombre de host**: tu-dominio.com (opcional)

### Paso 5: Configurar Application Pool
1. En IIS, ir a "Grupos de aplicaciones"
2. Seleccionar el pool de tu sitio
3. Click en "Configuración básica"
4. Cambiar **Versión de .NET CLR** a → **Sin código administrado**

### Paso 6: Configurar la Base de Datos
Editar el archivo `appsettings.json` en la carpeta publish:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR_SQL;Database=TicketsAndretich;User Id=tu_usuario;Password=tu_password;TrustServerCertificate=True;"
  }
}
```

### Paso 7: Crear la Base de Datos
Ejecutar en SQL Server Management Studio:
```sql
CREATE DATABASE TicketsAndretich;
```
Las tablas se crearán automáticamente al iniciar la aplicación (EF Core Migrations).

### Paso 8: Permisos de carpeta
```powershell
# Dar permisos de escritura a IIS
icacls "C:\inetpub\wwwroot\TicketsAndretich" /grant "IIS_IUSRS:(OI)(CI)M"
icacls "C:\inetpub\wwwroot\TicketsAndretich\App_Data" /grant "IIS_IUSRS:(OI)(CI)F"
```

### Paso 9: Verificar
1. Abrir navegador: `http://localhost` o `http://tu-servidor`
2. Iniciar sesión con: `admin@andretich.local` / `Admin123$`

---

## ☁️ Opción 2: Azure App Service (Hosting en la nube)

Azure es la nube de Microsoft, ideal para ASP.NET Core. Tiene un **tier gratuito** limitado.

### Paso 1: Crear cuenta de Azure
1. Ir a: https://azure.microsoft.com/free
2. Crear cuenta (incluye $200 USD de crédito gratis por 30 días)
3. Necesitarás una tarjeta de crédito (no se cobra en tier gratuito)

### Paso 2: Crear Azure SQL Database
1. En Azure Portal (portal.azure.com) → "Crear un recurso"
2. Buscar "SQL Database" → Crear
3. Configurar:
   - **Grupo de recursos**: Crear nuevo → `tickets-rg`
   - **Nombre base de datos**: `TicketsAndretichDB`
   - **Servidor**: Crear nuevo
     - Nombre: `tickets-server-tuusuario`
     - Autenticación SQL: usuario `tickets_admin`, contraseña segura
   - **Proceso**: Seleccionar **Básico** ($5/mes) o **Serverless** (pago por uso)
4. En "Redes" → Permitir acceso a servicios de Azure
5. Crear y esperar

### Paso 3: Crear App Service
1. Azure Portal → "Crear un recurso" → "App Service"
2. Configurar:
   - **Grupo de recursos**: `tickets-rg` (el mismo)
   - **Nombre**: `tickets-andretich` (será tu URL: tickets-andretich.azurewebsites.net)
   - **Publicar**: Código
   - **Runtime stack**: .NET 8 (LTS)
   - **Sistema operativo**: Windows
   - **Región**: La más cercana (Brazil South, East US)
   - **Plan de precios**: F1 (Gratis) o B1 ($13/mes)
3. Crear

### Paso 4: Configurar Connection String
1. Ir a tu App Service → "Configuración" → "Cadenas de conexión"
2. Agregar nueva:
   - **Nombre**: `DefaultConnection`
   - **Valor**: (obtener de Azure SQL → Cadenas de conexión)
   ```
   Server=tcp:tickets-server.database.windows.net,1433;Database=TicketsAndretichDB;User ID=tickets_admin;Password=TU_PASSWORD;Encrypt=true;
   ```
   - **Tipo**: SQLAzure
3. Guardar

### Paso 5: Deploy desde GitHub (automático)
1. En tu App Service → "Centro de implementación"
2. **Origen**: GitHub
3. Autorizar con tu cuenta GitHub
4. Seleccionar:
   - **Organización**: tblandretich
   - **Repositorio**: tickets
   - **Rama**: main
5. Guardar → Azure creará un GitHub Action automáticamente

### Paso 6: Ejecutar migraciones
1. Opción A: SSH desde Azure Portal
   ```bash
   cd site/wwwroot
   dotnet TicketsAndretich.Web.dll database update
   ```
2. Opción B: Ya se aplican automáticamente al iniciar (EF Core)

### Paso 7: Acceder
Tu aplicación estará en: `https://tickets-andretich.azurewebsites.net`

---

## 🌐 Opción 3: Subir código a GitHub (Backup/Control de versiones)

### Paso 1: Instalar Git
1. Descargar de: https://git-scm.com/downloads
2. Instalar con opciones por defecto
3. Verificar instalación:
```powershell
git --version
```

### Paso 2: Configurar Git (primera vez)
```powershell
git config --global user.name "Tu Nombre"
git config --global user.email "tu@email.com"
```

### Paso 3: Crear repositorio en GitHub
1. Ir a https://github.com
2. Click en "+" → "New repository"
3. Nombre: `tickets-andretich`
4. Privado o Público según prefieras
5. **NO** marcar "Add a README file"
6. Click "Create repository"

### Paso 4: Inicializar Git en el proyecto
```powershell
cd "C:\Users\Lucas\Desktop\Ticket\Local\TicketsAndretich.Web"

# Inicializar repositorio
git init

# Agregar todos los archivos
git add .

# Crear primer commit
git commit -m "Versión inicial - Sistema de Tickets Andretich"

# Conectar con GitHub (reemplaza TU_USUARIO)
git remote add origin https://github.com/TU_USUARIO/tickets-andretich.git

# Subir al repositorio
git branch -M main
git push -u origin main
```

### Paso 5: Autenticación en GitHub
- Si pide usuario/contraseña, usar un **Personal Access Token**:
  1. GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
  2. Generate new token → Seleccionar "repo" como scope
  3. Usar el token como contraseña

---

## 📁 Estructura de archivos para subir

```
TicketsAndretich.Web/
├── Areas/
├── Controllers/
├── Data/
├── Migrations/
├── Models/
├── Services/
├── Views/
├── wwwroot/
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
├── TicketsAndretich.Web.csproj
└── README.txt
```

**NO subir a GitHub:**
- `/bin/`
- `/obj/`
- `/publish/`
- `appsettings.Production.json` (si tiene secretos)

### Crear .gitignore
El proyecto ya debería tener un `.gitignore`, pero si no:
```gitignore
bin/
obj/
publish/
*.user
.vs/
App_Data/
```

---

## 🔒 Consideraciones de Seguridad para Producción

1. **Cambiar contraseña del admin** después del primer login
2. **Usar HTTPS** - Configurar certificado SSL en IIS
3. **Cambiar connection string** a credenciales seguras
4. **Respaldar la base de datos** regularmente
5. **Configurar OAuth2** en producción con tu propio Client ID/Secret

---

## 📞 Credenciales por defecto

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| admin@andretich.local | Admin123$ | Administrador |

---

## 🆘 Solución de Problemas

### Error 500.19 en IIS
- Instalar .NET 8.0 Hosting Bundle
- Ejecutar `iisreset`

### Error de conexión a SQL Server
- Verificar que SQL Server esté corriendo
- Verificar el connection string en appsettings.json
- Habilitar TCP/IP en SQL Server Configuration Manager

### La aplicación no inicia
- Revisar logs en: Event Viewer → Windows Logs → Application
- O habilitar stdout en web.config:
```xml
<aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

---

**¿Necesitas más ayuda? Revisa la documentación oficial:**
- [Desplegar en IIS](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/)
- [GitHub Docs](https://docs.github.com/es)
