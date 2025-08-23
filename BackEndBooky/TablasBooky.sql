-- =============================================
-- SCRIPT COMPLETO DE BASE DE DATOS BOOKY
-- =============================================

USE [master]
GO

-- Crear la base de datos si no existe
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Booky')
BEGIN
    CREATE DATABASE [Booky]
END
GO

USE [Booky]
GO

-- =============================================
-- TABLA: ROLES
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [IdRol] INT IDENTITY(1,1) PRIMARY KEY,
        [Nombre] VARCHAR(50) NOT NULL UNIQUE,
        [Descripcion] VARCHAR(255) NULL
    );

    -- Insertar roles básicos
    INSERT INTO [dbo].[Roles] ([Nombre], [Descripcion])
    VALUES 
    ('Cliente', 'Usuario que solicita servicios y agenda citas'),
    ('Profesional', 'Usuario que ofrece servicios y gestiona citas'),
    ('Admin', 'Usuario con privilegios de administración');
END
GO

-- =============================================
-- TABLA: USUARIOS
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE [dbo].[Usuarios] (
        [IdUsuario] INT IDENTITY(1,1) PRIMARY KEY,
        [IdRol] INT NOT NULL,
        [Cedula] VARCHAR(20) NOT NULL UNIQUE,
        [Nombre] VARCHAR(100) NOT NULL,
        [Email] VARCHAR(150) NOT NULL UNIQUE,
        [PasswordHash] VARCHAR(255) NOT NULL,
        [Telefono] VARCHAR(20) NULL,
        [EmailVerificado] BIT NOT NULL DEFAULT 0,
        [FechaRegistro] DATETIME NOT NULL DEFAULT GETDATE(),
        [Estado] BIT NOT NULL DEFAULT 1,
        [Bloqueado] BIT NOT NULL DEFAULT 0,
        [IntentosLoginFallidos] INT NOT NULL DEFAULT 0,
        [FechaUltimoIntentoFallido] DATETIME NULL,
        CONSTRAINT FK_Usuarios_Roles FOREIGN KEY ([IdRol]) REFERENCES [dbo].[Roles] ([IdRol])
    );
END
GO

-- =============================================
-- TABLA: CÓDIGOS DE VERIFICACIÓN
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CodigosVerificacion')
BEGIN
    CREATE TABLE [dbo].[CodigosVerificacion] (
        [IdCodigoVerificacion] INT IDENTITY(1,1) PRIMARY KEY,
        [IdUsuario] INT NOT NULL,
        [Codigo] VARCHAR(10) NOT NULL,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [FechaExpiracion] DATETIME NOT NULL,
        [Usado] BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_CodigosVerificacion_Usuarios FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuarios] ([IdUsuario])
    );
END
GO

-- =============================================
-- TABLA: CÓDIGOS DE RECUPERACIÓN
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CodigosRecuperacion')
BEGIN
    CREATE TABLE [dbo].[CodigosRecuperacion] (
        [IdCodigoRecuperacion] INT IDENTITY(1,1) PRIMARY KEY,
        [IdUsuario] INT NOT NULL,
        [Codigo] VARCHAR(10) NOT NULL,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [FechaExpiracion] DATETIME NOT NULL,
        [Usado] BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_CodigosRecuperacion_Usuarios FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuarios] ([IdUsuario])
    );
END
GO

-- =============================================
-- TABLA: INTENTOS DE LOGIN
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IntentosLogin')
BEGIN
    CREATE TABLE [dbo].[IntentosLogin](
        [IdIntentoLogin] INT IDENTITY(1,1) PRIMARY KEY,
        [IdUsuario] INT NULL,
        [Email] VARCHAR(150) NOT NULL,
        [IPAddress] VARCHAR(45) NULL,
        [FechaIntento] DATETIME NOT NULL DEFAULT GETDATE(),
        [Exitoso] BIT NOT NULL,
        [MotivoFallo] VARCHAR(100) NULL,
        CONSTRAINT FK_IntentosLogin_Usuarios FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuarios] ([IdUsuario])
    );
END
GO

-- =============================================
-- TABLA: BLOQUEOS DE USUARIO
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BloqueosUsuario')
BEGIN
    CREATE TABLE [dbo].[BloqueosUsuario](
        [IdBloqueo] INT IDENTITY(1,1) PRIMARY KEY,
        [IdUsuario] INT NOT NULL,
        [FechaBloqueo] DATETIME NOT NULL DEFAULT GETDATE(),
        [FechaDesbloqueo] DATETIME NULL,
        [MotivoBloqueo] VARCHAR(255) NOT NULL,
        [BloqueadoPor] INT NULL,
        [Activo] BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_BloqueosUsuario_Usuario FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuarios] ([IdUsuario]),
        CONSTRAINT FK_BloqueosUsuario_BloqueadoPor FOREIGN KEY ([BloqueadoPor]) REFERENCES [dbo].[Usuarios] ([IdUsuario])
    );
END
GO

-- =============================================
-- TABLA: PLANES DE SUSCRIPCIÓN
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Planes')
BEGIN
    CREATE TABLE [dbo].[Planes](
        [IdPlan] INT IDENTITY(1,1) PRIMARY KEY,
        [Nombre] VARCHAR(50) NOT NULL,
        [Descripcion] VARCHAR(500) NULL,
        [PrecioMensual] DECIMAL(10,2) NOT NULL,
        [PrecioAnual] DECIMAL(10,2) NULL,
        [MaxServicios] INT NOT NULL,
        [MaxClientes] INT NOT NULL,
        [MaxListaEspera] INT NOT NULL,
        [MaxPoliticaCancelacion] INT NOT NULL,
        [IncluyeEstadisticas] BIT NOT NULL,
        [IncluyeAnuncios] BIT NOT NULL,
        [Estado] BIT NOT NULL DEFAULT 1
    );

    -- Insertar planes iniciales
    INSERT INTO [dbo].[Planes] ([Nombre], [Descripcion], [PrecioMensual], [PrecioAnual], [MaxServicios], [MaxClientes], [MaxListaEspera], [MaxPoliticaCancelacion], [IncluyeEstadisticas], [IncluyeAnuncios]) 
    VALUES
    ('Gratuito', 'Plan básico para empezar', 0.00, 0.00, 5, 30, 0, 24, 0, 0),
    ('Básico', 'Plan intermedio para profesionales establecidos', 9.99, 99.00, 30, 120, 50, 999999, 0, 0),
    ('Premium', 'Plan completo con todas las funcionalidades', 39.99, 399.00, 300, 999999, 999999, 999999, 1, 1);
END
GO

-- =============================================
-- TABLA: SUSCRIPCIONES PROFESIONALES
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SuscripcionesProfesionales')
BEGIN
    CREATE TABLE [dbo].[SuscripcionesProfesionales](
        [IdSuscripcion] INT IDENTITY(1,1) PRIMARY KEY,
        [IdUsuario] INT NOT NULL,
        [IdPlan] INT NOT NULL,
        [FechaInicio] DATETIME NOT NULL,
        [FechaFin] DATETIME NOT NULL,
        [Estado] VARCHAR(20) NOT NULL,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_SuscripcionesProfesionales_Usuario FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuarios] ([IdUsuario]),
        CONSTRAINT FK_SuscripcionesProfesionales_Plan FOREIGN KEY ([IdPlan]) REFERENCES [dbo].[Planes] ([IdPlan])
    );
END
GO

-- =============================================
-- TABLA: PERFILES PROFESIONALES
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PerfilesProfesionales')
BEGIN
    CREATE TABLE [dbo].[PerfilesProfesionales](
        [IdPerfil] INT IDENTITY(1,1) PRIMARY KEY,
        [IdUsuario] INT NOT NULL,
        [Profesion] VARCHAR(100) NOT NULL,
        [Descripcion] TEXT NULL,
        [Direccion] VARCHAR(255) NULL,
        [Latitud] DECIMAL(10,8) NULL,
        [Longitud] DECIMAL(11,8) NULL,
        [CalificacionPromedio] DECIMAL(3,2) NULL,
        [TotalCalificaciones] INT NOT NULL DEFAULT 0,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [Estado] BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_PerfilesProfesionales_Usuario FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuarios] ([IdUsuario])
    );
END
GO

-- =============================================
-- TABLA: SERVICIOS
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Servicios')
BEGIN
    CREATE TABLE [dbo].[Servicios](
        [IdServicio] INT IDENTITY(1,1) PRIMARY KEY,
        [IdPerfil] INT NOT NULL,
        [Nombre] VARCHAR(100) NOT NULL,
        [Descripcion] VARCHAR(500) NULL,
        [DuracionMinutos] INT NULL,
        [Precio] DECIMAL(10,2) NOT NULL,
        [PermiteDescuento] BIT NOT NULL DEFAULT 0,
        [PorcentajeDescuento] DECIMAL(5,2) NULL,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [Estado] BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_Servicios_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil])
    );
END
GO

-- =============================================
-- TABLA: HORARIOS PROFESIONALES
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HorariosProfesionales')
BEGIN
    CREATE TABLE [dbo].[HorariosProfesionales](
        [IdHorario] INT IDENTITY(1,1) PRIMARY KEY,
        [IdPerfil] INT NOT NULL,
        [DiaSemana] TINYINT NOT NULL,
        [HoraInicio] TIME NOT NULL,
        [HoraFin] TIME NOT NULL,
        [Estado] BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_HorariosProfesionales_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil])
    );
END
GO

-- =============================================
-- TABLA: BLOQUES NO DISPONIBLES
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BloquesNoDisponibles')
BEGIN
    CREATE TABLE [dbo].[BloquesNoDisponibles](
        [IdBloque] INT IDENTITY(1,1) PRIMARY KEY,
        [IdPerfil] INT NOT NULL,
        [FechaInicio] DATETIME NOT NULL,
        [FechaFin] DATETIME NOT NULL,
        [Motivo] VARCHAR(255) NULL,
        [TipoBloque] VARCHAR(20) NOT NULL,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_BloquesNoDisponibles_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil])
    );
END
GO

-- =============================================
-- TABLA: CITAS
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Citas')
BEGIN
    CREATE TABLE [dbo].[Citas](
        [IdCita] INT IDENTITY(1,1) PRIMARY KEY,
        [IdPerfil] INT NOT NULL,
        [IdCliente] INT NOT NULL,
        [IdServicio] INT NOT NULL,
        [FechaCita] DATETIME NOT NULL,
        [DuracionMinutos] INT NULL,
        [PrecioAcordado] DECIMAL(10,2) NOT NULL,
        [Estado] VARCHAR(20) NOT NULL,
        [MensajeSolicitud] TEXT NULL,
        [MotivoRechazo] VARCHAR(255) NULL,
        [MotivoCancelacion] VARCHAR(255) NULL,
        [FechaSolicitud] DATETIME NOT NULL DEFAULT GETDATE(),
        [FechaRespuesta] DATETIME NULL,
        [ProbabilidadIncumplimiento] DECIMAL(5,2) NULL,
        CONSTRAINT FK_Citas_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil]),
        CONSTRAINT FK_Citas_Cliente FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Usuarios] ([IdUsuario]),
        CONSTRAINT FK_Citas_Servicio FOREIGN KEY ([IdServicio]) REFERENCES [dbo].[Servicios] ([IdServicio])
    );
END
GO

-- =============================================
-- TABLA: LISTA DE ESPERA
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ListaEspera')
BEGIN
    CREATE TABLE [dbo].[ListaEspera](
        [IdListaEspera] INT IDENTITY(1,1) PRIMARY KEY,
        [IdPerfil] INT NOT NULL,
        [IdCliente] INT NOT NULL,
        [IdServicio] INT NOT NULL,
        [FechaDeseada] DATETIME NULL,
        [RangoFechaInicio] DATETIME NULL,
        [RangoFechaFin] DATETIME NULL,
        [Prioridad] INT NOT NULL,
        [FechaRegistro] DATETIME NOT NULL DEFAULT GETDATE(),
        [Estado] VARCHAR(20) NOT NULL,
        CONSTRAINT FK_ListaEspera_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil]),
        CONSTRAINT FK_ListaEspera_Cliente FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Usuarios] ([IdUsuario]),
        CONSTRAINT FK_ListaEspera_Servicio FOREIGN KEY ([IdServicio]) REFERENCES [dbo].[Servicios] ([IdServicio])
    );
END
GO

-- =============================================
-- TABLA: POLÍTICAS DE CANCELACIÓN
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PoliticasCancelacion')
BEGIN
    CREATE TABLE [dbo].[PoliticasCancelacion](
        [IdPolitica] INT IDENTITY(1,1) PRIMARY KEY,
        [IdPerfil] INT NOT NULL,
        [TiempoMinimoHoras] INT NOT NULL,
        [PermiteCancelacionCliente] BIT NOT NULL DEFAULT 1,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [Estado] BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_PoliticasCancelacion_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil])
    );
END
GO

-- =============================================
-- TABLA: CALIFICACIONES
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Calificaciones')
BEGIN
    CREATE TABLE [dbo].[Calificaciones](
        [IdCalificacion] INT IDENTITY(1,1) PRIMARY KEY,
        [IdCita] INT NOT NULL,
        [IdCliente] INT NOT NULL,
        [IdPerfil] INT NOT NULL,
        [Puntuacion] TINYINT NOT NULL,
        [Comentario] TEXT NULL,
        [FechaCalificacion] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Calificaciones_Cita FOREIGN KEY ([IdCita]) REFERENCES [dbo].[Citas] ([IdCita]),
        CONSTRAINT FK_Calificaciones_Cliente FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Usuarios] ([IdUsuario]),
        CONSTRAINT FK_Calificaciones_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil])
    );
END
GO

-- =============================================
-- TABLA: POSICIONAMIENTO PREMIUM
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PosicionamientoPremium')
BEGIN
    CREATE TABLE [dbo].[PosicionamientoPremium](
        [IdPosicionamiento] INT IDENTITY(1,1) PRIMARY KEY,
        [IdPerfil] INT NOT NULL,
        [FechaInicio] DATETIME NOT NULL,
        [FechaFin] DATETIME NOT NULL,
        [MontoPagado] DECIMAL(10,2) NOT NULL,
        [Estado] VARCHAR(20) NOT NULL,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PosicionamientoPremium_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil])
    );
END
GO

-- =============================================
-- TABLA: CAMPAÑAS PUBLICITARIAS
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CampanasPublicitarias')
BEGIN
    CREATE TABLE [dbo].[CampanasPublicitarias](
        [IdCampana] INT IDENTITY(1,1) PRIMARY KEY,
        [IdPerfil] INT NOT NULL,
        [Titulo] VARCHAR(100) NOT NULL,
        [Descripcion] VARCHAR(500) NULL,
        [AlcanceMaximo] INT NOT NULL,
        [MontoPagado] DECIMAL(10,2) NOT NULL,
        [FechaInicio] DATETIME NOT NULL,
        [FechaFin] DATETIME NOT NULL,
        [Estado] VARCHAR(20) NOT NULL,
        [ImpresionesReales] INT NOT NULL DEFAULT 0,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_CampanasPublicitarias_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil])
    );
END
GO

-- =============================================
-- TABLA: TIPOS DE NOTIFICACIÓN
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TiposNotificacion')
BEGIN
    CREATE TABLE [dbo].[TiposNotificacion](
        [IdTipoNotificacion] INT IDENTITY(1,1) PRIMARY KEY,
        [Nombre] VARCHAR(50) NOT NULL,
        [Descripcion] VARCHAR(255) NULL,
        [Template] TEXT NULL
    );

    -- Insertar tipos de notificación iniciales
    INSERT INTO [dbo].[TiposNotificacion] ([Nombre], [Descripcion]) 
    VALUES
    ('Nueva Solicitud', 'Notificación de nueva solicitud de cita'),
    ('Cita Confirmada', 'Confirmación de aprobación de cita'),
    ('Recordatorio', 'Recordatorio de cita próxima'),
    ('Cancelación', 'Notificación de cancelación de cita'),
    ('Lista de Espera', 'Notificación de cambio en lista de espera'),
    ('Calificación', 'Notificación de nueva calificación recibida');
END
GO

-- =============================================
-- TABLA: NOTIFICACIONES
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notificaciones')
BEGIN
    CREATE TABLE [dbo].[Notificaciones](
        [IdNotificacion] INT IDENTITY(1,1) PRIMARY KEY,
        [IdUsuario] INT NOT NULL,
        [IdTipoNotificacion] INT NOT NULL,
        [Titulo] VARCHAR(255) NOT NULL,
        [Mensaje] TEXT NOT NULL,
        [Leida] BIT NOT NULL DEFAULT 0,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [FechaLectura] DATETIME NULL,
        CONSTRAINT FK_Notificaciones_Usuario FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuarios] ([IdUsuario]),
        CONSTRAINT FK_Notificaciones_Tipo FOREIGN KEY ([IdTipoNotificacion]) REFERENCES [dbo].[TiposNotificacion] ([IdTipoNotificacion])
    );
END
GO

-- =============================================
-- TABLA: HISTORIAL DE EMAILS
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HistorialEmails')
BEGIN
    CREATE TABLE [dbo].[HistorialEmails](
        [IdEmail] INT IDENTITY(1,1) PRIMARY KEY,
        [IdUsuario] INT NOT NULL,
        [EmailDestino] VARCHAR(150) NOT NULL,
        [Asunto] VARCHAR(255) NOT NULL,
        [Contenido] TEXT NOT NULL,
        [Enviado] BIT NOT NULL DEFAULT 0,
        [FechaEnvio] DATETIME NULL,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [ErrorEnvio] VARCHAR(500) NULL,
        CONSTRAINT FK_HistorialEmails_Usuario FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuarios] ([IdUsuario])
    );
END
GO

-- =============================================
-- TABLA: ESTADÍSTICAS PROFESIONALES
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EstadisticasProfesionales')
BEGIN
    CREATE TABLE [dbo].[EstadisticasProfesionales](
        [IdEstadistica] INT IDENTITY(1,1) PRIMARY KEY,
        [IdPerfil] INT NOT NULL,
        [Mes] TINYINT NOT NULL,
        [Año] INT NOT NULL,
        [TotalCitas] INT NOT NULL,
        [CitasCompletadas] INT NOT NULL,
        [CitasCanceladas] INT NOT NULL,
        [CitasReprogramadas] INT NOT NULL,
        [IngresosTotales] DECIMAL(12,2) NOT NULL,
        [CalificacionPromedio] DECIMAL(3,2) NULL,
        [TiempoPromedioRespuesta] INT NULL,
        CONSTRAINT FK_EstadisticasProfesionales_Perfil FOREIGN KEY ([IdPerfil]) REFERENCES [dbo].[PerfilesProfesionales] ([IdPerfil])
    );
END
GO

-- =============================================
-- ÍNDICES PARA OPTIMIZACIÓN DE RENDIMIENTO
-- =============================================

-- Índices para tabla de intentos de login
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_IntentosLogin_Email_Fecha')
BEGIN
    CREATE INDEX IX_IntentosLogin_Email_Fecha ON [dbo].[IntentosLogin] ([Email], [FechaIntento]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_IntentosLogin_Usuario_Fecha')
BEGIN
    CREATE INDEX IX_IntentosLogin_Usuario_Fecha ON [dbo].[IntentosLogin] ([IdUsuario], [FechaIntento]);
END
GO

-- Índices para citas
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Citas_Profesional_Fecha')
BEGIN
    CREATE INDEX IX_Citas_Profesional_Fecha ON [dbo].[Citas] ([IdPerfil], [FechaCita]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Citas_Cliente_Estado')
BEGIN
    CREATE INDEX IX_Citas_Cliente_Estado ON [dbo].[Citas] ([IdCliente], [Estado]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Citas_Estado_Fecha')
BEGIN
    CREATE INDEX IX_Citas_Estado_Fecha ON [dbo].[Citas] ([Estado], [FechaCita]);
END
GO

-- Índices para disponibilidad
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BloquesNoDisponibles_Perfil_Fechas')
BEGIN
    CREATE INDEX IX_BloquesNoDisponibles_Perfil_Fechas ON [dbo].[BloquesNoDisponibles] ([IdPerfil], [FechaInicio], [FechaFin]);
END
GO

-- Índices para búsquedas
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PerfilesProfesionales_Profesion')
BEGIN
    CREATE INDEX IX_PerfilesProfesionales_Profesion ON [dbo].[PerfilesProfesionales] ([Profesion]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Servicios_Precio')
BEGIN
    CREATE INDEX IX_Servicios_Precio ON [dbo].[Servicios] ([Precio]);
END
GO

-- =============================================
-- SCRIPT COMPLETADO EXITOSAMENTE
-- =============================================

PRINT 'Base de datos Booky creada y configurada correctamente.'
PRINT 'Todas las tablas, relaciones e índices han sido creados.'
PRINT 'Los datos iniciales han sido insertados.'
GO