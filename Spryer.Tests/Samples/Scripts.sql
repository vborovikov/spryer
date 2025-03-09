--@version 1.0.1

use SomeDb;
go

/*@execute-scalar Multiline(@Description nvarchar(100), 
    @Number Measure, @NumberValue Fractional, @NumberUnit DbEnum<MeasurementType>?, 
    @Quantity Measure, @QuantityValue Fractional, @QuantityUnit DbEnum<MeasurementType>?, 
    @AltQuantity Measure, @AltQuantityValue Fractional, @AltQuantityUnit DbEnum<MeasurementType>?)
*/
select 1;

--@script TestSelect
SELECT Id, Name, Email, Address 
FROM Users
WHERE Id = @Id;

--@