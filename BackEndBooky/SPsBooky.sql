-- =============================================
-- STORED PROCEDURES BOOKY
-- =============================================

USE [Booky]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- SP: GENERAR CÓDIGO DE RECUPERACIÓN
-- Descripción: Generar un código de recuperación para un usuario dado su correo electrónico
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SP_GENERAR_CODIGO_RECUPERACION]
    @CorreoElectronico NVARCHAR(255),  -- Correo que envía el cliente
    @SUCCESS BIT OUTPUT,               -- Salida: 1 = OK, 0 = Error
    @ERRORID INT OUTPUT                -- Salida: Código de error
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @IdUsuario INT;
    DECLARE @Codigo VARCHAR(10);
    DECLARE @FechaExpiracion DATETIME;

    -- Inicializar variables de salida
    SET @SUCCESS = 0;
    SET @ERRORID = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- ================================
        -- VALIDAR PARÁMETROS DE ENTRADA
        -- ================================
        IF (LTRIM(RTRIM(@CorreoElectronico)) = '' OR @CorreoElectronico IS NULL)
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 20001; -- Error: Correo no proporcionado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- BUSCAR EL USUARIO POR EMAIL
        -- ================================
        SELECT @IdUsuario = IdUsuario
        FROM [dbo].[Usuarios] 
        WHERE Email = @CorreoElectronico 
          AND Estado = 1;

        IF @IdUsuario IS NULL
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 20002; -- Error: Usuario no encontrado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- MARCAR CÓDIGOS ANTERIORES COMO USADOS
        -- ================================
        UPDATE [dbo].[CodigosRecuperacion]
        SET Usado = 1
        WHERE IdUsuario = @IdUsuario 
          AND Usado = 0;

        -- ================================
        -- GENERAR NUEVO CÓDIGO
        -- ================================
        SET @Codigo = RIGHT('000000' + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS VARCHAR(6)), 6);
        SET @FechaExpiracion = DATEADD(MINUTE, 15, GETDATE()); -- 15 minutos de validez

        -- ================================
        -- INSERTAR NUEVO CÓDIGO
        -- ================================
        INSERT INTO [dbo].[CodigosRecuperacion] (
            IdUsuario, 
            Codigo, 
            FechaCreacion, 
            FechaExpiracion, 
            Usado
        )
        VALUES (
            @IdUsuario, 
            @Codigo, 
            GETDATE(), 
            @FechaExpiracion, 
            0
        );

        COMMIT TRANSACTION;
        
        SET @SUCCESS = 1;
        SET @ERRORID = 0;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @SUCCESS = 0;
        SET @ERRORID = ERROR_NUMBER();
    END CATCH
END
GO

-- =============================================
-- SP: CAMBIAR CONTRASEÑA CON CÓDIGO
-- Descripción: Validar un código de recuperación y actualizar la contraseña de un usuario
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SP_CAMBIAR_CONTRASENA_CON_CODIGO]
    @Codigo VARCHAR(10),                 -- Código de recuperación
    @NuevaContrasenaHash NVARCHAR(255),  -- Contraseña ya hasheada
    @SUCCESS BIT OUTPUT,                 -- Salida: 1 = OK, 0 = Error
    @ERRORID INT OUTPUT                  -- Salida: Código de error
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdUsuario INT;
    DECLARE @CodigoValido BIT = 0;

    -- Inicializar variables de salida
    SET @SUCCESS = 0;
    SET @ERRORID = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- ================================
        -- VALIDAR PARÁMETROS DE ENTRADA
        -- ================================
        IF (@Codigo IS NULL OR LTRIM(RTRIM(@Codigo)) = '')
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 30001; -- Código no proporcionado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF (@NuevaContrasenaHash IS NULL OR LTRIM(RTRIM(@NuevaContrasenaHash)) = '')
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 30002; -- Contraseña no proporcionada
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- BUSCAR Y VALIDAR EL CÓDIGO
        -- ================================
        SELECT TOP 1 
            @IdUsuario = cr.IdUsuario,
            @CodigoValido = 1
        FROM [dbo].[CodigosRecuperacion] cr
        INNER JOIN [dbo].[Usuarios] u ON cr.IdUsuario = u.IdUsuario
        WHERE cr.Codigo = @Codigo
          AND cr.Usado = 0
          AND cr.FechaExpiracion >= GETDATE()
          AND u.Estado = 1
        ORDER BY cr.FechaCreacion DESC;

        -- Validar si el código existe y es válido
        IF @CodigoValido = 0 OR @IdUsuario IS NULL
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 30003; -- Código inválido, expirado o usuario inactivo
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- ACTUALIZAR CONTRASEÑA DEL USUARIO
        -- ================================
        UPDATE [dbo].[Usuarios]
        SET PasswordHash = @NuevaContrasenaHash,
            IntentosLoginFallidos = 0,
            Bloqueado = 0,
            FechaUltimoIntentoFallido = NULL
        WHERE IdUsuario = @IdUsuario;

        -- ================================
        -- MARCAR CÓDIGO COMO USADO
        -- ================================
        UPDATE [dbo].[CodigosRecuperacion]
        SET Usado = 1
        WHERE Codigo = @Codigo;

        COMMIT TRANSACTION;
        
        SET @SUCCESS = 1;
        SET @ERRORID = 0;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @SUCCESS = 0;
        SET @ERRORID = ERROR_NUMBER();
    END CATCH
END
GO

-- =============================================
-- SP: LOGIN DE USUARIO
-- Descripción: Autenticar usuario con controles de seguridad y bloqueo automático
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SP_LOGIN_USUARIO]
    @Email VARCHAR(150),              -- Email del usuario
    @PasswordHash VARCHAR(255),       -- Contraseña hasheada
    @IPAddress VARCHAR(45) = NULL,    -- IP del cliente (opcional)
    @IdUsuario INT OUTPUT,            -- ID del usuario autenticado
    @RolNombre VARCHAR(50) OUTPUT,    -- Nombre del rol del usuario
    @SUCCESS BIT OUTPUT,              -- Resultado: 1 = éxito, 0 = error
    @ERRORID INT OUTPUT               -- Código de error
AS
BEGIN
    SET NOCOUNT ON;

    -- ================================
    -- CONFIGURACIÓN DE SEGURIDAD
    -- ================================
    DECLARE @MaxIntentosFallidos INT = 5;
    DECLARE @TiempoBloqueoMinutos INT = 30;
    
    -- Variables de control
    DECLARE @IntentosActuales INT = 0;
    DECLARE @EstaBloqueado BIT = 0;
    DECLARE @FechaUltimoIntento DATETIME;
    DECLARE @UsuarioExiste BIT = 0;
    DECLARE @UsuarioTempId INT;
    DECLARE @PasswordCorrecta BIT = 0;
    DECLARE @EmailVerificado BIT = 0;

    -- Inicializar variables de salida
    SET @IdUsuario = NULL;
    SET @RolNombre = NULL;
    SET @SUCCESS = 0;
    SET @ERRORID = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- ================================
        -- VALIDAR PARÁMETROS OBLIGATORIOS
        -- ================================
        IF @Email IS NULL OR LTRIM(RTRIM(@Email)) = ''
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 10001; -- Email no proporcionado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @PasswordHash IS NULL OR LTRIM(RTRIM(@PasswordHash)) = ''
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 10002; -- Contraseña no proporcionada
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- BUSCAR Y VALIDAR USUARIO
        -- ================================
        SELECT 
            @UsuarioTempId = IdUsuario,
            @IntentosActuales = IntentosLoginFallidos,
            @EstaBloqueado = Bloqueado,
            @FechaUltimoIntento = FechaUltimoIntentoFallido,
            @EmailVerificado = EmailVerificado,
            @UsuarioExiste = 1
        FROM [dbo].[Usuarios] 
        WHERE Email = @Email 
          AND Estado = 1;

        -- Si el usuario no existe
        IF @UsuarioExiste = 0 OR @UsuarioTempId IS NULL
        BEGIN
            -- Registrar intento fallido sin ID de usuario
            INSERT INTO [dbo].[IntentosLogin] (
                IdUsuario, Email, IPAddress, FechaIntento, Exitoso, MotivoFallo
            )
            VALUES (
                NULL, @Email, @IPAddress, GETDATE(), 0, 'Usuario no encontrado'
            );

            SET @SUCCESS = 0;
            SET @ERRORID = 10003; -- Usuario no encontrado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- VERIFICAR SI ESTÁ BLOQUEADO
        -- ================================
        IF @EstaBloqueado = 1
        BEGIN
            -- Verificar si el tiempo de bloqueo ha expirado
            IF @FechaUltimoIntento IS NOT NULL 
               AND DATEDIFF(MINUTE, @FechaUltimoIntento, GETDATE()) >= @TiempoBloqueoMinutos
            BEGIN
                -- Desbloquear automáticamente
                UPDATE [dbo].[Usuarios] 
                SET Bloqueado = 0,
                    IntentosLoginFallidos = 0,
                    FechaUltimoIntentoFallido = NULL
                WHERE IdUsuario = @UsuarioTempId;
                
                SET @EstaBloqueado = 0;
                SET @IntentosActuales = 0;
            END
            ELSE
            BEGIN
                -- La cuenta sigue bloqueada
                INSERT INTO [dbo].[IntentosLogin] (
                    IdUsuario, Email, IPAddress, FechaIntento, Exitoso, MotivoFallo
                )
                VALUES (
                    @UsuarioTempId, @Email, @IPAddress, GETDATE(), 0, 'Cuenta bloqueada'
                );

                SET @SUCCESS = 0;
                SET @ERRORID = 10004; -- Cuenta bloqueada
                ROLLBACK TRANSACTION;
                RETURN;
            END
        END

        -- ================================
        -- VERIFICAR EMAIL VERIFICADO
        -- ================================
        IF @EmailVerificado = 0
        BEGIN
            INSERT INTO [dbo].[IntentosLogin] (
                IdUsuario, Email, IPAddress, FechaIntento, Exitoso, MotivoFallo
            )
            VALUES (
                @UsuarioTempId, @Email, @IPAddress, GETDATE(), 0, 'Email no verificado'
            );

            SET @SUCCESS = 0;
            SET @ERRORID = 10006; -- Email no verificado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- VERIFICAR CONTRASEÑA
        -- ================================
        IF EXISTS (
            SELECT 1 FROM [dbo].[Usuarios] 
            WHERE IdUsuario = @UsuarioTempId 
              AND PasswordHash = @PasswordHash
        )
        BEGIN
            SET @PasswordCorrecta = 1;
        END
        ELSE
        BEGIN
            SET @PasswordCorrecta = 0;
        END

        -- ================================
        -- MANEJAR CONTRASEÑA INCORRECTA
        -- ================================
        IF @PasswordCorrecta = 0
        BEGIN
            SET @IntentosActuales = @IntentosActuales + 1;
            
            -- Verificar si debe bloquear la cuenta
            IF @IntentosActuales >= @MaxIntentosFallidos
            BEGIN
                -- BLOQUEAR la cuenta
                UPDATE [dbo].[Usuarios] 
                SET IntentosLoginFallidos = @IntentosActuales,
                    FechaUltimoIntentoFallido = GETDATE(),
                    Bloqueado = 1
                WHERE IdUsuario = @UsuarioTempId;

                -- Registrar intento que causó el bloqueo
                INSERT INTO [dbo].[IntentosLogin] (
                    IdUsuario, Email, IPAddress, FechaIntento, Exitoso, MotivoFallo
                )
                VALUES (
                    @UsuarioTempId, @Email, @IPAddress, GETDATE(), 0, 'Cuenta bloqueada por intentos fallidos'
                );
                
                SET @SUCCESS = 0;
                SET @ERRORID = 10005; -- Cuenta bloqueada por intentos fallidos
            END
            ELSE
            BEGIN
                -- Solo incrementar contador
                UPDATE [dbo].[Usuarios] 
                SET IntentosLoginFallidos = @IntentosActuales,
                    FechaUltimoIntentoFallido = GETDATE()
                WHERE IdUsuario = @UsuarioTempId;

                -- Registrar intento fallido
                INSERT INTO [dbo].[IntentosLogin] (
                    IdUsuario, Email, IPAddress, FechaIntento, Exitoso, MotivoFallo
                )
                VALUES (
                    @UsuarioTempId, @Email, @IPAddress, GETDATE(), 0, 'Contraseña incorrecta'
                );
                
                SET @SUCCESS = 0;
                SET @ERRORID = 10003; -- Contraseña incorrecta
            END

            COMMIT TRANSACTION;
            RETURN;
        END

        -- ================================
        -- ÉXITO EN EL LOGIN
        -- ================================
        SELECT 
            @IdUsuario = u.IdUsuario,
            @RolNombre = r.Nombre
        FROM [dbo].[Usuarios] u
        INNER JOIN [dbo].[Roles] r ON u.IdRol = r.IdRol
        WHERE u.IdUsuario = @UsuarioTempId;

        -- Resetear contador de intentos fallidos
        UPDATE [dbo].[Usuarios] 
        SET IntentosLoginFallidos = 0,
            FechaUltimoIntentoFallido = NULL,
            Bloqueado = 0
        WHERE IdUsuario = @IdUsuario;

        -- Registrar intento exitoso
        INSERT INTO [dbo].[IntentosLogin] (
            IdUsuario, Email, IPAddress, FechaIntento, Exitoso, MotivoFallo
        )
        VALUES (
            @IdUsuario, @Email, @IPAddress, GETDATE(), 1, NULL
        );

        COMMIT TRANSACTION;
        
        SET @SUCCESS = 1;
        SET @ERRORID = 0;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @SUCCESS = 0;
        SET @ERRORID = ERROR_NUMBER();
    END CATCH
END
GO

-- =============================================
-- SP: REGISTRAR USUARIO
-- Descripción: Crear un nuevo usuario en el sistema
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SP_REGISTRAR_USUARIO]
    @Cedula VARCHAR(20),              -- Cédula del usuario
    @Nombre VARCHAR(100),             -- Nombre completo
    @Email VARCHAR(150),              -- Email único
    @PasswordHash VARCHAR(255),       -- Contraseña hasheada
    @Telefono VARCHAR(20) = NULL,     -- Teléfono (opcional)
    @IdRol INT = 1,                   -- Rol (1=Cliente por defecto)
    @IdUsuario INT OUTPUT,            -- ID del usuario creado
    @SUCCESS BIT OUTPUT,              -- Resultado: 1 = éxito, 0 = error
    @ERRORID INT OUTPUT               -- Código de error
AS
BEGIN
    SET NOCOUNT ON;

    -- Inicializar variables de salida
    SET @IdUsuario = NULL;
    SET @SUCCESS = 0;
    SET @ERRORID = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- ================================
        -- VALIDAR PARÁMETROS OBLIGATORIOS
        -- ================================
        IF @Cedula IS NULL OR LTRIM(RTRIM(@Cedula)) = ''
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 40001; -- Cédula no proporcionada
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Nombre IS NULL OR LTRIM(RTRIM(@Nombre)) = ''
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 40002; -- Nombre no proporcionado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Email IS NULL OR LTRIM(RTRIM(@Email)) = ''
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 40003; -- Email no proporcionado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @PasswordHash IS NULL OR LTRIM(RTRIM(@PasswordHash)) = ''
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 40004; -- Contraseña no proporcionada
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- VALIDAR UNICIDAD
        -- ================================
        IF EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE Cedula = @Cedula)
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 40005; -- Cédula ya existe
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE Email = @Email)
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 40006; -- Email ya existe
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- VALIDAR QUE EL ROL EXISTE
        -- ================================
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE IdRol = @IdRol)
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 40007; -- Rol no válido
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- CREAR EL USUARIO
        -- ================================
        INSERT INTO [dbo].[Usuarios] (
            IdRol, Cedula, Nombre, Email, PasswordHash, Telefono,
            EmailVerificado, FechaRegistro, Estado, Bloqueado,
            IntentosLoginFallidos, FechaUltimoIntentoFallido
        )
        VALUES (
            @IdRol, @Cedula, @Nombre, @Email, @PasswordHash, @Telefono,
            0, GETDATE(), 1, 0, 0, NULL
        );

        SET @IdUsuario = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
        
        SET @SUCCESS = 1;
        SET @ERRORID = 0;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @SUCCESS = 0;
        SET @ERRORID = ERROR_NUMBER();
    END CATCH
END
GO

-- =============================================
-- SP: GENERAR CÓDIGO DE VERIFICACIÓN
-- Descripción: Generar código para verificar email de usuario
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SP_GENERAR_CODIGO_VERIFICACION]
    @IdUsuario INT,                   -- ID del usuario
    @Codigo VARCHAR(10) OUTPUT,       -- Código generado
    @SUCCESS BIT OUTPUT,              -- Resultado: 1 = éxito, 0 = error
    @ERRORID INT OUTPUT               -- Código de error
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FechaExpiracion DATETIME;

    -- Inicializar variables de salida
    SET @Codigo = NULL;
    SET @SUCCESS = 0;
    SET @ERRORID = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- ================================
        -- VALIDAR QUE EL USUARIO EXISTE
        -- ================================
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE IdUsuario = @IdUsuario AND Estado = 1)
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 50001; -- Usuario no válido
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- MARCAR CÓDIGOS ANTERIORES COMO USADOS
        -- ================================
        UPDATE [dbo].[CodigosVerificacion]
        SET Usado = 1
        WHERE IdUsuario = @IdUsuario 
          AND Usado = 0;

        -- ================================
        -- GENERAR NUEVO CÓDIGO
        -- ================================
        SET @Codigo = RIGHT('000000' + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS VARCHAR(6)), 6);
        SET @FechaExpiracion = DATEADD(MINUTE, 15, GETDATE()); -- 15 minutos de validez

        -- ================================
        -- INSERTAR NUEVO CÓDIGO
        -- ================================
        INSERT INTO [dbo].[CodigosVerificacion] (
            IdUsuario, 
            Codigo, 
            FechaCreacion, 
            FechaExpiracion, 
            Usado
        )
        VALUES (
            @IdUsuario, 
            @Codigo, 
            GETDATE(), 
            @FechaExpiracion, 
            0
        );

        COMMIT TRANSACTION;
        
        SET @SUCCESS = 1;
        SET @ERRORID = 0;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @SUCCESS = 0;
        SET @ERRORID = ERROR_NUMBER();
        SET @Codigo = NULL;
    END CATCH
END
GO

-- =============================================
-- SP: VERIFICAR EMAIL CON CÓDIGO
-- Descripción: Verificar email de usuario usando código de verificación
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SP_VERIFICAR_EMAIL_CON_CODIGO]
    @Codigo VARCHAR(10),              -- Código de verificación
    @SUCCESS BIT OUTPUT,              -- Resultado: 1 = éxito, 0 = error
    @ERRORID INT OUTPUT               -- Código de error
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdUsuario INT;
    DECLARE @CodigoValido BIT = 0;

    -- Inicializar variables de salida
    SET @SUCCESS = 0;
    SET @ERRORID = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- ================================
        -- VALIDAR PARÁMETRO
        -- ================================
        IF (@Codigo IS NULL OR LTRIM(RTRIM(@Codigo)) = '')
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 60001; -- Código no proporcionado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- BUSCAR Y VALIDAR EL CÓDIGO
        -- ================================
        SELECT TOP 1 
            @IdUsuario = cv.IdUsuario,
            @CodigoValido = 1
        FROM [dbo].[CodigosVerificacion] cv
        INNER JOIN [dbo].[Usuarios] u ON cv.IdUsuario = u.IdUsuario
        WHERE cv.Codigo = @Codigo
          AND cv.Usado = 0
          AND cv.FechaExpiracion >= GETDATE()
          AND u.Estado = 1
          AND u.EmailVerificado = 0
        ORDER BY cv.FechaCreacion DESC;

        -- Validar si el código existe y es válido
        IF @CodigoValido = 0 OR @IdUsuario IS NULL
        BEGIN
            SET @SUCCESS = 0;
            SET @ERRORID = 60002; -- Código inválido, expirado o ya verificado
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- ================================
        -- MARCAR EMAIL COMO VERIFICADO
        -- ================================
        UPDATE [dbo].[Usuarios]
        SET EmailVerificado = 1
        WHERE IdUsuario = @IdUsuario;

        -- ================================
        -- MARCAR CÓDIGO COMO USADO
        -- ================================
        UPDATE [dbo].[CodigosVerificacion]
        SET Usado = 1
        WHERE Codigo = @Codigo;

        COMMIT TRANSACTION;
        
        SET @SUCCESS = 1;
        SET @ERRORID = 0;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @SUCCESS = 0;
        SET @ERRORID = ERROR_NUMBER();
    END CATCH
END
GO

-- =============================================
-- SCRIPT COMPLETADO EXITOSAMENTE
-- =============================================

PRINT 'Stored Procedures de Booky creados correctamente.'
PRINT 'Procedimientos disponibles:'
PRINT '  - SP_GENERAR_CODIGO_RECUPERACION'
PRINT '  - SP_CAMBIAR_CONTRASENA_CON_CODIGO'
PRINT '  - SP_LOGIN_USUARIO'
PRINT '  - SP_REGISTRAR_USUARIO'
PRINT '  - SP_GENERAR_CODIGO_VERIFICACION'
PRINT '  - SP_VERIFICAR_EMAIL_CON_CODIGO'
GO