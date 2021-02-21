using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using DapperSample.Models;
using Microsoft.Extensions.Caching.Memory;

namespace DapperSample.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _memoryCache;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IMemoryCache memoryCache, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _memoryCache = memoryCache;
        }



        public T GetCache<T>(string key, Func<T> action)
        {
            object cacheItem = _memoryCache.Get(key);
            if (cacheItem is T)
                return (T)cacheItem;

            T items = action();
            _memoryCache.Set(key, items, new MemoryCacheEntryOptions { AbsoluteExpiration = DateTime.Now.AddMinutes(1) });
            return items;
        }

        private List<Product> LoadProducts()
        {
            var sql = "Select * From [Production].[Product];";
            using (IDbConnection dbConnection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                if (dbConnection.State != ConnectionState.Open)
                    dbConnection.Open();
                
                var productList = dbConnection.Query<Product>(sql).ToList();
                return productList;
            }
        }


        [HttpGet]
        public IActionResult GetProducts()
        {
            string key = "productList";
            var result = GetCache<List<Product>>(key, () => LoadProducts());
            return Ok();
        }


        [HttpGet]
        public IActionResult Get()
        {

            using (IDbConnection db = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                if (db.State != ConnectionState.Open)
                    db.Open();

                #region Dapper-Execute
                string sql = "";
                int affected = 0;
                /*
                    Insert, Update, Delete
                    parametre isimleri sql cümlesindeki parameterelerle aynı olması gerekiyor.
                */

                /*
                                 sql = @"INSERT INTO dbo.TestTable (Name, SurName, Phone, Age, BirthDate)
                                                  Values (@Name, @SurName, @Phone, @Age, @BirthDate);";

                                var affected = db.Execute(sql, new
                                {
                                    Name = "Burak",
                                    SurName = "Karadağ",
                                    Phone = "5545111111",
                                    Age = 30,
                                    BirthDate = new DateTime(1990, 12, 07)
                                });

                */
                /* Insert Many */

                /*
                sql = @"INSERT INTO dbo.TestTable (Name, SurName, Phone, Age, BirthDate)
                                  Values (@Name, @SurName, @Phone, @Age, @BirthDate);";

                object[] objList = new object[11];
                for (var i = 0; i < 10; i++)
                {
                    objList[i] = new
                    {
                        Name = "Burak-" + i,
                        SurName = "Karadağ",
                        Phone = "5545111111",
                        Age = 30 + i,
                        BirthDate = new DateTime(1990, 12, 07)
                    };
                }

                objList[10] = new
                {
                    Name = "Burak-11",
                    SurName = "Karadağ",
                    Phone = "5545111111",
                    Age = 30 + 11,
                    BirthDate = ""
                };

                affected = db.Execute(sql, objList);

                */

                /* Update Many */

                /*
                   sql = "Update dbo.TestTable Set Phone = @PhoneTest Where Id=@IdReal";
                   var paramsArray = new[]
                   {
                       new { IdReal = 2, PhoneTest = "11111111111"},
                       new { IdReal = 3, PhoneTest = "55555555555"}
                   };

                   affected = db.Execute(sql, paramsArray);


               */

                /* Delete */
                /*

                sql = "Delete From dbo.TestTable Where Id=@Id";
                affected = db.Execute(sql, new[]
                {
                    new { Id = 1 },
                    new { Id = 2 }
                });
                */

                #endregion Dapper-Execute


                #region Dapper-ExecuteReader

                // Execute reader db üzerinden blok data okur
                /*
                sql = "SELECT * FROM Sales.Customer;";
                var reader = db.ExecuteReader(sql);
                DataTable table = new DataTable();
                table.Load(reader);
                */

                #endregion Dapper-ExecuteReader

                #region Dapper-Scaler

                // executeScaler >> dbden tek kolon okumak için kullanılır.
                // ResultSet içinden ilk satır ilk kolonunu döner

                sql = "Select Count(*) From Sales.Customer;";
                var customerCount = db.ExecuteScalar<int>(sql);


                #endregion Dapper-Scaler


                #region Query

                // ResultSet'i alır , Map eder.

                //Dynamic
                // sql = "Select * From [Production].[UnitMeasure];";
                // var uniqMeasure = db.Query(sql);

                // // Strongly Model Bind
                // sql = "Select * From [Production].[UnitMeasure];";
                // IEnumerable<UnitMeasure> unitMeasures = db.Query<UnitMeasure>(sql);

                #endregion @uery


                #region QueryFirst

                // sql = "Select * From [Sales].[SalesOrderDetail] Where ProductId = @ProductId";

                // var result = db.QueryFirst<SalesorderDetail>(sql, new { ProductId = 707 });

                #endregion QueryFirst

                #region QueryMultiple

                /*
                                sql = @"Select * From [Production].[Product] Where ProductId = @ProductId;
                                        Select * From [Sales].[SalesOrderDetail] Where ProductId = @ProductId;";

                                var multipleQuery = db.QueryMultiple(sql, new { ProductId = 707 });

                                var products = multipleQuery.Read<Product>();
                                var salesOrderDetails = multipleQuery.Read<SalesorderDetail>().ToList();
                */
                #endregion QueryMultiple

                #region Transaction 

                /*
                using (var transaction = db.BeginTransaction())
                {
                    sql = @"INSERT INTO dbo.TestTable (Name, SurName, Phone, Age, BirthDate)
                                                  Values (@Name, @SurName, @Phone, @Age, @BirthDate);";

                    affected = db.Execute(sql, new
                    {
                        Name = "Deneme",
                        SurName = "Karadağ",
                        Phone = "5545111111",
                        Age = 30,
                        BirthDate = new DateTime(1990, 12, 07)
                    }, transaction);


                    throw new ArgumentNullException();

                    UnitMeasure unitMeasure = new UnitMeasure()
                    {
                        UnitMeasureCode = "NEW",
                        Name = "NewUnit",
                        ModifiedDate = DateTime.Now
                    };

                    sql = @"Insert into [production].[UnitMeasure] (UnitMeasureCode, Name, ModifiedDate)
                                Values (@UnitMeasureCode, @Name, @ModifiedDate)";

                    affected = db.Execute(sql, unitMeasure, transaction);

                    transaction.Commit();

                }
                */

                /*
                using(var transactionScope = new TransactionScope())
                {
                     sql = @"INSERT INTO dbo.TestTable (Name, SurName, Phone, Age, BirthDate)
                                                  Values (@Name, @SurName, @Phone, @Age, @BirthDate);";

                    affected = db.Execute(sql, new
                    {
                        Name = "Deneme",
                        SurName = "Karadağ",
                        Phone = "5545111111",
                        Age = 30,
                        BirthDate = new DateTime(1990, 12, 07)
                    });


                    throw new ArgumentNullException();

                    UnitMeasure unitMeasure = new UnitMeasure()
                    {
                        UnitMeasureCode = "NEW",
                        Name = "NewUnit",
                        ModifiedDate = DateTime.Now
                    };

                    sql = @"Insert into [production].[UnitMeasure] (UnitMeasureCode, Name, ModifiedDate)
                                Values (@UnitMeasureCode, @Name, @ModifiedDate)";

                    affected = db.Execute(sql, unitMeasure);

                    transactionScope.Complete();
                }

                */

                #endregion Transaction

            }

            return Ok();
        }
    }
}
