--@version 0.0.1

/*

--@script ShouldntExist
select *
from aTable;
--@

*/

--@ script  PragmaSpacesFullMeta  (@Param1, @Param2)
select t.*
from aTable t
where t.Column1 = @Param1 and t.Column2 = @Param2;

--@

--@version 0.0.2
--@

--@script  "Name Space"  (@Param1, @Param2)
select t.*
from aTable t
where t.Column1 = @Param1 and t.Column2 = @Param2;

--@

--@ script  PragmaSpacesSimpleMeta
select t.*
from aTable t
where t.Column1 = @Param1 and t.Column2 = @Param2;

--@
