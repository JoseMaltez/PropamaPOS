INSERT INTO Roles (Nombre, Descripcion) VALUES 
('Admin', 'Administrador del sistema con todos los permisos'),
('Empleado', 'Empleado con permisos limitados');


INSERT INTO Usuarios(NombreUsuario, ContraHash, ContraSalt, Id_Rol) VALUES 
('jmaltez', 'PtqkopJqDeynTJQM2gOSRorOTWdDskcNTfTfKBlx2eM=','Q8bhbqFQUl/hKExCVzF0wdDU/fV+6JVdrx8Mbql8YzYMq3KdhDguys/6nb7N1I8dpIbnl4/maXEniKvglVhbxw==',1)

insert into Empleados (Nombre, Apellido, Correo, Telefono, FechaContratacion, Activo, Id_Usuario) values
('Jose','Maltez','josemaltezv@gmail.com','59512640',GETDATE(),1,1)

select * from Roles
select * from Usuarios
select * from Empleados


