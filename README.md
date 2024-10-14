# sv.db
slothful vic try make db code be sloth flash, because vic lazy, so it can not be fastest, but maybe like flash

## What can sv.db do

### 1. db to entity

Like [DapperAOT](https://github.com/DapperLib/DapperAOT), use [Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) generating the necessary code during build to help you use sql more easy.

And Theoretically, you also can do [Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=windows%2Cnet8)

Code exmples:

``` csharp
public async Task<object> OldWay()
{
    var a = factory.GetConnection(StaticInfo.Demo);
    using var dd = await a.ExecuteReaderAsync("""
SELECT count(1)
FROM Weather;
SELECT *
FROM Weather;
""");
    var t = await dd.QueryFirstOrDefaultAsync<int>();
    var r = await dd.QueryAsync<string>().ToListAsync();
    return new { TotalCount = t, Rows = r };
}
```

### 2. make select more easy and more complex condition

With define some simple rules for select, we can convert select to db / api / es ....

Theoretically, we can do like:

```
http query string / body  |------>  select statement    |------>  db (sqlite / mysql/ sqlserver / PostgreSQL)
Expression code           |------>                      |------>  es
                                                        |------>  mongodb
                                                        |------>  more .....
```

So we can use less code to do some simple select

#### 2.1 complex condition and page for api

##### Code exmples:

``` csharp
[HttpGet]
public async Task<object> Selects()
{
    return await this.QueryByParamsAsync<Weather>();
}

[Db("Demo")]
[Table(nameof(Weather))]
public class Weather
{
    [Select]
    public string Name { get; set; }

    [Select(Field = "Value")]
    public string V { get; set; }
}
```

You can use such query string to query api

```
curl --location 'http://localhost:5259/weather?where=not (name like '%e%')&TotalCount=true'
```

Response 
``` json
{
    "totalCount": 1,
    "rows": [
        {
            "name": "H",
            "v": "mery!"
        }
    ]
}
```

##### 2.2 complex condition and page for Expression code

##### Code exmples:

You can use such code to query Weather which name no Contains 'e'

``` csharp
[HttpGet("expr")]
public async Task<object> DoSelects()
{
    return await factory.From<Weather>().Where(i => !i.Name.Like("e")).WithTotalCount().ExecuteQueryAsync<dynamic>();
}

[Db("Demo")]
[Table(nameof(Weather))]
public class Weather
{
    [Select]
    public string Name { get; set; }

    [Select(Field = "Value as v")]
    public string V { get; set; }
}
```

## Doc

### Query in api

Both has func support use query string or body to query

body or query string will map to `Dictionary<string, string>` to handle

#### operater

such filter operater just make api more restful (`Where=urlencode(complex condition)` will be more better)

- `{{nl}}`  is null
    - query string `?name={{nl}}`
    - body `{"name":"{{nl}}"}`
- `{{eq}}`  Equal =
    - query string `?name=xxx`
    - body `{"name":"xxx"}`
- `{{lt}}`  LessThan or Equal <=
    - query string `?age={{lt}}30`
    - body `{"age":"{{lt}}30"}`
- `{{le}}`  LessThan <
    - query string `?age={{le}}30`
    - body `{"age":"{{le}}30"}`
- `{{gt}}`  GreaterThan or Equal  >=
    - query string `?age={{gt}}30`
    - body `{"age":"{{gt}}30"}`
- `{{gr}}`  GreaterThan >
    - query string `?age={{gr}}30`
    - body `{"age":"{{gr}}30"}`
- `{{nq}}`  Not Equal !=
    - query string `?age={{nq}}30`
    - body `{"age":"{{nq}}30"}`
- `{{lk}}`  Prefix Like 'e%'
    - query string `?name={{lk}}e`
    - body `{"name":"{{lk}}e"}`
- `{{rk}}`  Suffix Like '%e'
    - query string `?name={{rk}}e`
    - body `{"name":"{{rk}}e"}`
- `{{kk}}`  Like '%e%'
    - query string `?name={{kk}}e`
    - body `{"name":"{{kk}}e"}`
- `{{in}}`  in array (bool/number/string)
    - query string `?name={{in}}[true,false]`
    - body `{"name":"{{in}}[\"s\",\"sky\"]"}`
- `{{no}}`  not
    - query string `?age={{no}}{{lt}}30`
    - body `{"age":"{{no}}{{lt}}30"}`

#### Func Fields:

- `Fields`   return some Fields , no Fields or `Fields=*` is return all
    - query string `?Fields=name,age,json(data,'$.age')`
    - body `{"Fields":"name,age,json(data,'$.age')"}`
- `TotalCount`   return total count
    - query string `?TotalCount=true`
    - body `{"TotalCount":"true"}`
- `NoRows`   no return rows
    - query string `?NoRows=true`
    - body `{"NoRows":"true"}`
- `Offset`   Offset Rows index
    - query string `?Offset=10`
    - body `{"Offset":10}`
- `Rows`   Take Rows count, default is 10
    - query string `?Rows=100`
    - body `{"Rows":100}`
- `OrderBy` sort result
    - query string `?OrderBy=name asc,age desc,json(data,'$.age') desc`
    - body `{"OrderBy":"name asc,age desc,json(data,'$.age') desc"}`
- `Where`   complex condition filter
    - query string `?Where=urlencode( not(name like 'H%') or name like '%v%' )`
    - body `{"Where":"not(name like 'H%') or name like '%v%'"}`
    - operaters
        - bool
            - example  `true` or `false`
        - number
            - example  `12323` or `1.324` or `-44.4`
        - string
            - example  `'sdsdfa'` or `'sds\'dfa'` or `"dsdsdsd"` or `"fs\"dsf"`
        - ` = null`  is null
            - example ` name = null`
        - ` != null`  is not null
            - example ` name != null`
        - `=`  Equal
            - example ` name = 'sky'`
        - `<=`  LessThan or Equal 
            - example ` age <= 30`
        - `<`  LessThan 
            - example ` age < 30`
        - `>=`  GreaterThan or Equal
            - example ` age >= 30`
        - `>`  GreaterThan 
            - example ` age > 30`
        - `!=`  Not Equal 
            - example ` age != 30`
        - `like 'e%'`  Prefix Like
            - example ` name like 'xx%'`
        - `like '%e'`  Suffix Like
            - example ` name like '%xx'`
        - `like '%e%'`  Like
            - example ` name like '%xx%'`
        - `in ()`  in array (bool/number/string)
            - example `in (1,2,3)` or `in ('sdsdfa','sdfa')` or `in (true,false)`
        - `not`
            - example ` not( age <= 30 )`
        - `and`
            - example `  age <= 30 and age > 60`
        - `or`
            - example ` age <= 30 or age > 60`
        - `()`
            - example ` (age <= 30 or age > 60) and name = 'killer'`
     - support json
        - example ` json(data,'$.age') > 60` 