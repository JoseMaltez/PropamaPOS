INSERT INTO Roles (Nombre, Descripcion) VALUES 
('Admin', 'Administrador del sistema con todos los permisos'),
('Empleado', 'Empleado con permisos limitados');


INSERT INTO Usuarios(NombreUsuario, PasswordHash, Id_Rol) VALUES 
('jmaltez', '$2a$11$raztPBPlUdSw3niSgDmV3u4NnjXCRuDWEX8ufJsnXWX1b0xC26pfK',1)

insert into Empleados (Nombre, Apellido, Correo, Telefono, FechaContratacion, Activo, Id_Usuario) values
('Jose','Maltez','josemaltezv@gmail.com','59512640',GETDATE(),1,1)

insert into UnidadesMedida (Nombre, Abreviatura) values
('Unidad', 'ud'),
('Caja', 'cj'),
('Ciento','c'),
('Medio Ciento','mc'),
('Resma','res')

insert into Categorias (Nombre, Descripcion) values
('Material Escolar y de Oficina','Productos ampliamente usados en ámbitos escolares y en trabajos de oficina'),
('Servicio','Servicios ofrecidos por el negocio'),
('Tecnología y Accesorios','Productos referentes a tecnología y electronicos'),
('Materiales de manualidades','Productos utilizados en la creación de manualidades, dibujo y pintura')

insert into Proveedores (Nombre, Telefono, Correo, Direccion, Activo) values ('Tulan', '70656000', 'tulan@gmail.com', '6ta Avenida, Zona 1', 1)

select * from Roles
select * from Usuarios
select * from Empleados
select * from UnidadesMedida
select * from Categorias
select * from Proveedores


