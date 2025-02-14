--@version 1.0.1

use SomeDb;
go

--@script TestSelect
SELECT Id, Name, Email, Address 
FROM Users
WHERE Id = @Id;

--@