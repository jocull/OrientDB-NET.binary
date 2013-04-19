﻿using System;
using System.Collections.Generic;
using Orient.Client.Protocol;
using Orient.Client.Protocol.Operations;

namespace Orient.Client
{
    public class ODatabase : IDisposable
    {
        private bool _containsConnection;
        private Connection _connection;

        public OSqlCreate Create { get { return new OSqlCreate(_connection); } }
        public OSqlInsert Insert { get { return new OSqlInsert(_connection); } }
        public OSqlUpdate Update { get { return new OSqlUpdate(_connection); } }

        public ODatabase(string alias)
        {
            _connection = OClient.ReleaseConnection(alias);
            _containsConnection = true;
        }

        public List<OCluster> GetClusters()
        {
            return _connection.Document.GetField<List<OCluster>>("Clusters");
        }

        #region Select

        public OSqlSelect Select(string projection)
        {
            return new OSqlSelect(_connection).Select(projection);
        }

        public OSqlSelect Select(params string[] projections)
        {
            return new OSqlSelect(_connection).Select(projections);
        }

        #endregion

        #region Query

        public List<ODocument> Query(string sql)
        {
            return Query(sql, "*:0");
        }

        public List<ODocument> Query(string sql, string fetchPlan)
        {
            CommandPayload payload = new CommandPayload();
            payload.Type = CommandPayloadType.Sql;
            payload.Text = sql;
            payload.NonTextLimit = -1;
            payload.FetchPlan = fetchPlan;
            payload.SerializedParams = new byte[] { 0 };

            Command operation = new Command();
            operation.OperationMode = OperationMode.Asynchronous;
            operation.ClassType = CommandClassType.Idempotent;
            operation.CommandPayload = payload;

            ODocument document = _connection.ExecuteOperation<Command>(operation);

            return document.GetField<List<ODocument>>("Content");
        }

        #endregion

        public OCommandResult Command(string sql)
        {
            CommandPayload payload = new CommandPayload();
            payload.Type = CommandPayloadType.Sql;
            payload.Text = sql;
            payload.NonTextLimit = -1;
            payload.FetchPlan = "";
            payload.SerializedParams = new byte[] { 0 };

            Command operation = new Command();
            operation.OperationMode = OperationMode.Synchronous;
            operation.ClassType = CommandClassType.NonIdempotent;
            operation.CommandPayload = payload;

            ODocument document = _connection.ExecuteOperation<Command>(operation);

            return new OCommandResult(document);
        }

        public void Close()
        {
            if (_containsConnection)
            {
                if (_connection.IsReusable)
                {
                    OClient.ReturnConnection(_connection);
                }
                else
                {
                    _connection.Dispose();
                }

                _containsConnection = false;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
