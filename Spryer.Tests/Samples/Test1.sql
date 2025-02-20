--@version 1.0.1

use SomeDb;
go

--@script TestSelect1
SELECT Id, Name, Email, Address 
FROM Users
WHERE Id = @Id;

--@