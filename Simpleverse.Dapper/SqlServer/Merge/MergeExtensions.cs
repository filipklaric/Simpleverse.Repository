﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using System.Linq;
using System.Reflection;

namespace Simpleverse.Dapper.SqlServer.Merge
{
	public static class MergeExtensions
	{
		public async static Task<int> UpsertAsync<T>(
			this SqlConnection connection,
			T entitiesToUpsert,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<MergeKeyOptions> key = null
		)
			where T : class
		{
			return await connection.UpsertAsync(
				new[] { entitiesToUpsert },
				transaction: transaction,
				commandTimeout: commandTimeout,
				key: key
			);
		}

		public async static Task<int> MergeAsync<T>(
			this SqlConnection connection,
			T entitiesToMerge,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<MergeKeyOptions> key = null,
			Action<MergeActionOptions<T>> matched = null,
			Action<MergeActionOptions<T>> notMatchedByTarget = null,
			Action<MergeActionOptions<T>> notMatchedBySource = null
		)
			where T : class
		{
			return await connection.MergeBulkAsync(
				new[] { entitiesToMerge },
				transaction: transaction,
				commandTimeout: commandTimeout,
				key: key,
				matched: matched,
				notMatchedByTarget: notMatchedByTarget,
				notMatchedBySource: notMatchedBySource
			);
		}

		/// <summary>
		/// Updates entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
		/// </summary>
		/// <typeparam name="T">Type to be updated</typeparam>
		/// <param name="connection">Open SqlConnection</param>
		/// <param name="entitiesToUpsert">Entity to be updated</param>
		/// <param name="transaction">The transaction to run under, null (the default) if none</param>
		/// <param name="commandTimeout">Number of seconds before command execution timeout</param>
		/// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
		public async static Task<int> UpsertBulkAsync<T>(
			this SqlConnection connection,
			IEnumerable<T> entitiesToUpsert,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<MergeKeyOptions> key = null
		) where T : class
		{
			var typeMeta = TypeMeta.Get<T>();

			return await connection.MergeBulkAsync<T>(
				entitiesToUpsert,
				transaction,
				commandTimeout,
				sqlBulkCopy: sqlBulkCopy,
				key: key,
				matched: matched => matched.Update(),
				notMatchedByTarget: notMatchedByTarget => notMatchedByTarget.Insert()
			);
		}

		/// <summary>
		/// Merges entity in table "Ts", checks if the entity is modified if the entity is tracked by the Get() extension.
		/// </summary>
		/// <typeparam name="T">Type to be updated</typeparam>
		/// <param name="connection">Open SqlConnection</param>
		/// <param name="entitiesToMerge">Entity to be updated</param>
		/// <param name="transaction">The transaction to run under, null (the default) if none</param>
		/// <param name="commandTimeout">Number of seconds before command execution timeout</param>
		/// <returns>true if updated, false if not found or not modified (tracked entities)</returns>
		public async static Task<int> MergeBulkAsync<T>(
			this SqlConnection connection,
			IEnumerable<T> entitiesToMerge,
			SqlTransaction transaction = null,
			int? commandTimeout = null,
			Action<SqlBulkCopy> sqlBulkCopy = null,
			Action<MergeKeyOptions> key = null,
			Action<MergeActionOptions<T>> matched = null,
			Action<MergeActionOptions<T>> notMatchedByTarget = null,
			Action<MergeActionOptions<T>> notMatchedBySource = null
		) where T : class
		{
			if (entitiesToMerge == null)
				throw new ArgumentNullException(nameof(entitiesToMerge));

			var entityCount = entitiesToMerge.Count();
			if (entityCount == 0)
				return 0;

			if (entityCount == 1)
				return await connection.MergeAsync(
					entitiesToMerge.FirstOrDefault(),
					transaction: transaction,
					commandTimeout: commandTimeout,
					matched: matched,
					notMatchedByTarget: notMatchedByTarget,
					notMatchedBySource: notMatchedBySource
				);

			var typeMeta = TypeMeta.Get<T>();
			if (typeMeta.PropertiesKey.Count == 0 && typeMeta.PropertiesExplicit.Count == 0)
				throw new ArgumentException("Entity must have at least one [Key] or [ExplicitKey] property");

			var wasClosed = connection.State == System.Data.ConnectionState.Closed;
			if (wasClosed) connection.Open();

			var (source, parameters) = await connection.BulkSource<T>(
				entitiesToMerge,
				typeMeta.Properties,
				transaction: transaction,
				sqlBulkCopy: sqlBulkCopy
			);

			var sb = new StringBuilder($@"
				MERGE INTO {typeMeta.TableName} AS Target
				USING {source} AS Source
				ON ({OnColumns(typeMeta, keyAction: key).ColumnListEquals(" AND ")})"
			);
			sb.AppendLine();

			MergeMatchResult.Matched.Format(typeMeta, matched, sb);
			MergeMatchResult.NotMatchedBySource.Format(typeMeta, notMatchedBySource, sb);
			MergeMatchResult.NotMatchedByTarget.Format(typeMeta, notMatchedByTarget, sb);
			//MergeOutputFormat(typeMeta.PropertiesKey.Union(typeMeta.PropertiesComputed).ToList(), sb);
			sb.Append(";");

			var merged = await connection.ExecuteAsync(sb.ToString(), param: parameters, commandTimeout: commandTimeout, transaction: transaction);

			if (wasClosed) connection.Close();

			return merged;
		}

		public static IEnumerable<string> OnColumns(TypeMeta typeMeta, Action<MergeKeyOptions> keyAction = null)
		{
			var options = new MergeKeyOptions();
			if (keyAction == null)
				options.ColumnsByPropertyInfo(typeMeta.PropertiesKeyAndExplicit);
			else
				keyAction(options);

			return options.Columns;
		}

		public static void Format<T>(this MergeMatchResult result, TypeMeta typeMeta, Action<MergeActionOptions<T>> optionsAction, StringBuilder sb)
		{
			if (optionsAction == null)
				return;

			var options = new MergeActionOptions<T>();
			optionsAction(options);
			if (options.Action == MergeAction.None)
				return;

			switch (result)
			{
				case MergeMatchResult.Matched:
					sb.AppendLine("WHEN MATCHED");
					break;
				case MergeMatchResult.NotMatchedBySource:
					sb.AppendLine("WHEN NOT MATCHED BY SOURCE");
					break;
				case MergeMatchResult.NotMatchedByTarget:
					sb.AppendLine("WHEN NOT MATCHED BY TARGET");
					break;
			}

			if (!string.IsNullOrEmpty(options.Condition))
				sb.AppendFormat(" AND ({0})", options.Condition);
			sb.AppendLine(" THEN");

			options.Format(sb);
		}

		private static void MergeOutputFormat(IEnumerable<PropertyInfo> properties, StringBuilder sb)
		{
			if (properties.Any())
			{
				sb.AppendFormat("OUTPUT {0}", properties.ColumnList("Inserted"));
				sb.AppendLine();
			}
		}
	}
}
