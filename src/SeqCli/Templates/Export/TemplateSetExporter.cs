﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model;
using Seq.Api.Model.Monitoring;
using Seq.Api.Model.Retention;
using Seq.Api.Model.Signals;
using Seq.Api.Model.SqlQueries;
using Seq.Api.Model.Workspaces;

#nullable enable

namespace SeqCli.Templates.Export
{
    class TemplateSetExporter
    {
        readonly SeqConnection _connection;
        readonly HashSet<string?> _include;
        readonly string _outputDir;

        public TemplateSetExporter(SeqConnection connection, HashSet<string?> include, string outputDir)
        {
            _connection = connection;
            _include = include;
            _outputDir = outputDir;
        }
        
        public async Task ExportTemplateSet()
        {
            var templateValueMap = new TemplateValueMap();
            templateValueMap.MapNonNullAsArg<DashboardEntity>(nameof(DashboardEntity.OwnerId), "ownerId");
            templateValueMap.MapNonNullAsArg<SignalEntity>(nameof(SignalEntity.OwnerId), "ownerId");
            templateValueMap.MapNonNullAsArg<SqlQueryEntity>(nameof(SqlQueryEntity.OwnerId), "ownerId");
            templateValueMap.MapNonNullAsArg<WorkspaceEntity>(nameof(WorkspaceEntity.OwnerId), "ownerId");
            templateValueMap.MapNonNullAsArg<AlertPart>(nameof(AlertPart.NotificationAppInstanceId), "notificationAppInstanceId");
            templateValueMap.MapAsReference<SignalExpressionPart>(nameof(SignalExpressionPart.SignalId));
            templateValueMap.MapAsReference<WorkspaceEntity>(nameof(WorkspaceEntity.Content.DashboardIds));
            templateValueMap.MapAsReference<WorkspaceEntity>(nameof(WorkspaceEntity.Content.QueryIds));
            templateValueMap.MapAsReference<WorkspaceEntity>(nameof(WorkspaceEntity.Content.SignalIds));
            
            await ExportTemplates<SignalEntity>(
                id => _connection.Signals.FindAsync(id),
                () => _connection.Signals.ListAsync(shared: true),
                signal => signal.Title,
                templateValueMap);
            
            await ExportTemplates<SqlQueryEntity>(
                id => _connection.SqlQueries.FindAsync(id),
                () => _connection.SqlQueries.ListAsync(shared: true),
                query => query.Title,
                templateValueMap);
            
            await ExportTemplates<DashboardEntity>(
                id => _connection.Dashboards.FindAsync(id),
                () => _connection.Dashboards.ListAsync(shared: true),
                dashboard => dashboard.Title,
                templateValueMap);
            
            await ExportTemplates<WorkspaceEntity>(
                id => _connection.Workspaces.FindAsync(id),
                () => _connection.Workspaces.ListAsync(shared: true),
                workspace => workspace.Title,
                templateValueMap);
            
            await ExportTemplates<RetentionPolicyEntity>(
                id => _connection.RetentionPolicies.FindAsync(id),
                () => _connection.RetentionPolicies.ListAsync(),
                retentionPolicy => retentionPolicy.Id.Replace("retentionpolicy-", ""),
                templateValueMap);
        }
        
        async Task ExportTemplates<TEntity>(
            Func<string, Task<TEntity>> findEntity,
            Func<Task<List<TEntity>>> listEntities,
            Func<TEntity, string> getTitle,
            TemplateValueMap templateValueMap)
            where TEntity : Entity
        {
            List<TEntity> entities;
            if (!_include.Any())
            {
                entities = await listEntities();
            }
            else
            {
                var idPrefix = IdPrefix<TEntity>();
                entities = new List<TEntity>();
                foreach (var id in _include.Where(i => i != null && i.StartsWith(idPrefix)))
                {
                    entities.Add(await findEntity(id!));
                }
            }
            
            foreach (var entity in entities)
            {
                var filename = OutputFilename<SignalEntity>(getTitle(entity));
                templateValueMap.AddReferencedTemplate(entity.Id, Path.GetFileName(filename));
                await TemplateWriter.WriteTemplateAsync(filename, entity, templateValueMap);
            }
        }

        string OutputFilename<TEntity>(string title) where TEntity : Entity
        {
            var pathSafeTitle = new string(title.Select(c => c != ':' && c != '/' && c != '\\' ? c : '_').ToArray());
            return Path.Combine(_outputDir, $"{IdPrefix<TEntity>()}{pathSafeTitle}.{TemplateWriter.TemplateFileExtension}");
        }

        static string IdPrefix<TEntity>() where TEntity : Entity
        {
            return typeof(TEntity).Name.ToLowerInvariant().Replace("entity", "") + "-";
        }
    }
}
