﻿using Dapper;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PMS.WebAPI.Repo
{
    public class Repository<T> : IRepository<T> where T : class
    {

        private readonly IConfiguration _config;
        private readonly string Connectionstring = "DefaultConnection";
        private readonly string _tableName;

        public Repository(IConfiguration config, string tableName)
        {          
            _config = config;
            _tableName = tableName;
        }
        

        private SqlConnection SqlConnection()
        {
            return new SqlConnection(_config.GetConnectionString(Connectionstring));
        }

        private IDbConnection CreateConnection()
        {
            var conn = SqlConnection();
            conn.Open();
            return conn;
        }
        private IEnumerable<PropertyInfo> GetProperties => typeof(T).GetProperties();

       

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<T>($"SELECT * FROM {_tableName}");
            }
        }


        //Report Get
        public async Task<IEnumerable<T>> GetAsync(string insertQuery, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
        {

            using (var connection = CreateConnection())
            {
              return   await connection.QueryAsync<T>(insertQuery, null,null,null, commandType);              
            }
        }

        public async Task DeleteRowAsync(int id)
        {
            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync($"DELETE FROM {_tableName} WHERE Id=@Id", new { Id = id });
            }
        }
        public async Task<T> GetAsync(int id)
        {
            using (var connection = CreateConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<T>($"SELECT * FROM {_tableName} WHERE Id=@Id", new { Id = id });
                if (result == null)
                    throw new KeyNotFoundException($"{"Projects"} with id [{id}] could not be found.");
                return result;
            }
        }
        public async Task<int> SaveRangeAsync(IEnumerable<T> list)
        {
            var inserted = 0;
            var query = GenerateInsertQuery();
            using (var connection = CreateConnection())
            {
                inserted += await connection.ExecuteAsync(query, list);
            }
            return inserted;
        }

        private static List<string> GenerateListOfProperties(IEnumerable<PropertyInfo> listOfProperties)
        {
            return (from prop in listOfProperties
                    let attributes = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    where attributes.Length <= 0 || (attributes[0] as DescriptionAttribute)?.Description != "ignore"
                    select prop.Name).ToList();
        }

        // Create Project and Task Post
        public async Task InsertAsync(string insertQuery, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
        {           
            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(insertQuery, parms, null, null, commandType);             
            }
        }

        //public async Task InsertAsync(T t)
        //{
        //    var insertQuery = GenerateInsertQuery();
        //    using (var connection = CreateConnection())
        //    {
        //        await connection.ExecuteAsync(insertQuery, t);
        //    }
        //}
        private string GenerateInsertQuery()
        {
            var insertQuery = new StringBuilder($"INSERT INTO {_tableName} ");

            insertQuery.Append("(");
            var properties = GenerateListOfProperties(GetProperties);
            properties.ForEach(prop => { insertQuery.Append($"[{prop}],"); });
            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(") VALUES (");
            properties.ForEach(prop => { insertQuery.Append($"@{prop},"); });
            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(")");
            return insertQuery.ToString();
        }

        public async Task UpdateAsync(T t)
        {
            var updateQuery = GenerateUpdateQuery();
            using (var connection = CreateConnection())
            {
                await connection.ExecuteAsync(updateQuery, t);
            }
        }
        private string GenerateUpdateQuery()
        {
            var updateQuery = new StringBuilder($"UPDATE {"Projects"} SET ");
            var properties = GenerateListOfProperties(GetProperties);
            properties.ForEach(property =>
            {
                if (!property.Equals("Id"))
                {
                    updateQuery.Append($"{property}=@{property},");
                }
            });
            updateQuery.Remove(updateQuery.Length - 1, 1); //remove last comma
            updateQuery.Append(" WHERE Id=@Id");
            return updateQuery.ToString();
        }
    }
}
