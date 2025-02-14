--@version 1.0.0

use SomeDb;
go

--@script TestSelect
SELECT Id, Name, Email, Address 
FROM Users
WHERE Id = @Id;

--@