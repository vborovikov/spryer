--@version 1.0.2

use SomeDb;
go

--@script TestSelect2
SELECT Id, Name, Email, Address 
FROM Users
WHERE Id = @Id;

--@