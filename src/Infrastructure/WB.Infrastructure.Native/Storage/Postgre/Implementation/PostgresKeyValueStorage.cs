﻿using System;
using System.Collections.Generic;
using System.Data;
using Humanizer;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using WB.Core.GenericSubdomains.Portable.Services;

namespace WB.Infrastructure.Native.Storage.Postgre.Implementation
{
    internal abstract class PostgresKeyValueStorage<TEntity> 
        where TEntity: class
    {
        protected readonly string connectionString;
        protected readonly string tableName = typeof(TEntity).Name.Pluralize();

        private readonly ILogger logger;

        public PostgresKeyValueStorage(string connectionString, ILogger logger)
        {
            this.connectionString = connectionString;
            this.logger = logger;
            this.EnshureTableExists();
        }

        public TEntity GetById(string id)
        {
            string queryResult;
            using (var command = new NpgsqlCommand())
            {
                string commandText = string.Format("SELECT value FROM {0} WHERE id = :id", this.tableName);

                command.CommandText = commandText;
                var parameter = new NpgsqlParameter("id", NpgsqlDbType.Varchar) { Value = id };
                command.Parameters.Add(parameter);

                queryResult = (string) this.ExecuteScalar(command);
            }

            if (queryResult != null)
            {
                return JsonConvert.DeserializeObject<TEntity>(queryResult, JsonSerializerSettings);
            }

            return null;
        }

        protected abstract object ExecuteScalar(IDbCommand command);
        protected abstract int ExecuteNonQuery(IDbCommand command);
        
        public void Remove(string id)
        {
            int queryResult;
            using (var command = new NpgsqlCommand())
            {
                command.CommandText = string.Format("DELETE FROM {0} WHERE id = :id", this.tableName);
                var parameter = new NpgsqlParameter("id", NpgsqlDbType.Varchar) { Value = id };
                command.Parameters.Add(parameter);

                queryResult = this.ExecuteNonQuery(command);
            }
            if (queryResult > 1)
            {
                throw new Exception(
                    string.Format(
                        "Unexpected row count of deleted records. Expected to delete 1 row, but affected {0} number of rows",
                        queryResult));
            }
        }

        public void Store(TEntity view, string id)
        {
            object existsResult;
            using (var existsCommand = new NpgsqlCommand())
            {
                existsCommand.CommandText = string.Format("SELECT 1 FROM {0} WHERE id = :id LIMIT 1", this.tableName);

                var idParameter = new NpgsqlParameter("id", NpgsqlDbType.Varchar) { Value = id };

                existsCommand.Parameters.Add(idParameter);

                existsResult = this.ExecuteScalar(existsCommand);
            }

            var existing = existsResult != null;

            string commandText;
            if (existing)
            {
                commandText = string.Format("UPDATE {0} SET value = :value WHERE id = :id", this.tableName);
            }
            else
            {
                commandText = string.Format("INSERT INTO {0} VALUES(:id, :value)", this.tableName);
            }


            int queryResult;
            using (var upsertCommand = new NpgsqlCommand())
            {
                upsertCommand.CommandText = commandText;

                var parameter = new NpgsqlParameter("id", NpgsqlDbType.Varchar) { Value = id };
                string serializedValue = JsonConvert.SerializeObject(view, Formatting.None, JsonSerializerSettings);
                var valueParameter = new NpgsqlParameter("value", NpgsqlDbType.Json) { Value = serializedValue };

                upsertCommand.Parameters.Add(parameter);
                upsertCommand.Parameters.Add(valueParameter);

                queryResult = this.ExecuteNonQuery(upsertCommand);
            }
            if (queryResult > 1)
            {
                throw new Exception(string.Format("Unexpected row count of deleted records. Expected to delete not more than 1 row, but affected {0} number of rows", queryResult));
            }
        }

        public virtual void BulkStore(List<Tuple<TEntity, string>> bulk)
        {
            try
            {
                this.FastBulkStore(bulk);
            }
            catch (Exception exception)
            {
                this.logger.Warn($"Failed to store bulk of {bulk.Count} entities of type {this.ViewType.Name} using fast way. Switching to slow way.", exception);

                this.SlowBulkStore(bulk);
            }
        }

        public void Clear()
        {
            this.EnshureTableExists();

            using (var command = new NpgsqlCommand())
            {
                command.CommandText = string.Format("DELETE FROM {0}", this.tableName);
                this.ExecuteNonQuery(command);
            }
        }

        public void Dispose()
        {
        }

        public Type ViewType
        {
            get { return typeof(TEntity); }
        }

        public string GetReadableStatus()
        {
            return "Postgres K/V :/";
        }

        private void FastBulkStore(List<Tuple<TEntity, string>> bulk)
        {
            this.EnshureTableExists();

            using (var connection = new NpgsqlConnection(this.connectionString))
            {
                connection.Open();
                using (var writer = connection.BeginBinaryImport($"COPY {this.tableName}(id, value) FROM STDIN BINARY;"))
                {
                    foreach (var item in bulk)
                    {
                        writer.StartRow();
                        writer.Write(item.Item2, NpgsqlDbType.Text); // write Id

                        var serializedValue = JsonConvert.SerializeObject(item.Item1, Formatting.None, JsonSerializerSettings);
                        writer.Write(serializedValue, NpgsqlDbType.Json); // write value
                    }
                }
            }
        }

        private void SlowBulkStore(List<Tuple<TEntity, string>> bulk)
        {
            foreach (var tuple in bulk)
            {
                var entity = tuple.Item1;
                var id = tuple.Item2;

                this.Store(entity, id);
            }
        }

        protected void EnshureTableExists()
        {
            using (var connection = new NpgsqlConnection(this.connectionString))
            {
                connection.Open();
                var command = @"CREATE TABLE IF NOT EXISTS " + this.tableName + @" (
    id        text PRIMARY KEY,
    value       JSON NOT NULL
)";
                using (var sqlCommand = connection.CreateCommand())
                {
                    sqlCommand.CommandText = command;
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        protected static JsonSerializerSettings JsonSerializerSettings
        {
            get
            {
                return new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
            }
        }
    }
}