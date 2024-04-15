﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simpleverse.Repository.Db.Entity
{
	public class ProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		: IProjectedEntity<TProjection, TModel, TUpdate, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TUpdate : class
		where TOptions : DbQueryOptions, new()
	{
		protected ConstructorInfo _constructorMethod;
		protected readonly Func<TModel, TProjection> _creator;
		protected readonly Entity<TModel, TUpdate, TFilter, TOptions> _entity;

		public ProjectedEntity(Entity<TModel, TUpdate, TFilter, TOptions> entity)
		{
			_entity = entity;
		}

		public ProjectedEntity(Entity<TModel, TUpdate, TFilter, TOptions> entity, Func<TModel, TProjection> creator)
			: this(entity)
		{
			_creator = creator;
		}

		public Task<int> AddAsync(
			IDbConnection connection,
			IEnumerable<TProjection> models,
			Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null,
			IDbTransaction transaction = null
		)
		{
			return _entity.AddAsync(connection, models.Select(x => x.Model), OutputMapRedirect(models, outputMap), transaction: transaction);
		}

		public Task<int> AddAsync(TProjection model, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null)
		{
			return _entity.AddAsync(model.Model, OutputMapRedirect(new[] { model }, outputMap));
		}

		public Task<int> AddAsync(IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null)
		{
			return _entity.AddAsync(models.Select(x => x.Model), OutputMapRedirect(models, outputMap));
		}

		public Task<int> DeleteAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			return _entity.DeleteAsync(connection, filterSetup, optionsSetup, transaction: transaction);
		}

		public Task<bool> DeleteAsync(IDbConnection connection, TProjection model, IDbTransaction transaction = null)
		{
			return _entity.DeleteAsync(connection, model.Model, transaction: transaction);
		}

		public Task<int> DeleteAsync(IDbConnection connection, IEnumerable<TProjection> models, IDbTransaction transaction = null)
		{
			return _entity.DeleteAsync(connection, models.Select(x => x.Model), transaction: transaction);
		}

		public Task<int> DeleteAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			return _entity.DeleteAsync(filterSetup, optionsSetup);
		}

		public Task<bool> DeleteAsync(TProjection model)
		{
			return _entity.DeleteAsync(model.Model);
		}

		public Task<int> DeleteAsync(IEnumerable<TProjection> models)
		{
			return _entity.DeleteAsync(models.Select(x => x.Model));
		}

		public Task<bool> ExistsAsync(IDbConnection connection, Action<TFilter> filterSetup = null, IDbTransaction transaction = null)
		{
			return _entity.ExistsAsync(connection, filterSetup, transaction: transaction);
		}

		public Task<bool> ExistsAsync(Action<TFilter> filterSetup = null)
		{
			return _entity.ExistsAsync(filterSetup);
		}

		public async Task<TProjection> GetAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			var model = await _entity.GetAsync(connection, filterSetup, optionsSetup, transaction: transaction);
			if (model == null)
				return default;

			return Instance(model);
		}

		public Task<T> GetAsync<T>(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			return _entity.GetAsync<T>(connection, filterSetup, optionsSetup, transaction: transaction);
		}

		public async Task<TProjection> GetAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			var model = await _entity.GetAsync(filterSetup, optionsSetup);
			if (model == null)
				return default;

			return Instance(model);
		}

		public Task<T> GetAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			return _entity.GetAsync<T>(filterSetup, optionsSetup);
		}

		public async Task<IEnumerable<TProjection>> ListAsync(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			var models = await _entity.ListAsync(connection, filterSetup, optionsSetup, transaction);
			if (models == null)
				return default;

			return models.Select(Instance);
		}

		public Task<IEnumerable<T>> ListAsync<T>(IDbConnection connection, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
		{
			return _entity.ListAsync<T>(connection, filterSetup, optionsSetup, transaction: transaction);
		}

		public async Task<IEnumerable<TProjection>> ListAsync(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null)
		{
			var models = await _entity.ListAsync(connection, filter, options, transaction);
			if (models == null)
				return default;

			return models.Select(Instance);
		}

		public Task<IEnumerable<T>> ListAsync<T>(IDbConnection connection, TFilter filter, TOptions options, IDbTransaction transaction = null)
		{
			return _entity.ListAsync<T>(connection, filter, options, transaction: transaction);
		}

		public async Task<IEnumerable<TProjection>> ListAsync(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			var models = await _entity.ListAsync(filterSetup, optionsSetup);
			if (models == null)
				return default;

			return models.Select(Instance);
		}

		public Task<IEnumerable<T>> ListAsync<T>(Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
		{
			return _entity.ListAsync<T>(filterSetup, optionsSetup);
		}

		public async Task<IEnumerable<TProjection>> ListAsync(TFilter filter, TOptions options)
		{
			var models = await _entity.ListAsync(filter, options);
			if (models == null)
				return default;

			return models.Select(Instance);
		}

		public Task<IEnumerable<T>> ListAsync<T>(TFilter filter, TOptions options)
		{
			return _entity.ListAsync<T>(filter, options);
		}

		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columName, Action<TFilter> filterSetup, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MaxAsync<TResult>(connection, columName, filterSetup, transaction: transaction);

		public Task<TResult?> MaxAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MaxAsync<TResult>(connection, columnName, transaction: transaction);

		public Task<TResult?> MaxAsync<TResult>(string columName, Action<TFilter> filterSetup) where TResult : struct
			=> _entity.MaxAsync<TResult>(columName, filterSetup);

		public Task<TResult?> MaxAsync<TResult>(string columnName) where TResult : struct
			=> _entity.MaxAsync<TResult>(columnName);

		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columName, Action<TFilter> filterSetup, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MinAsync<TResult>(connection, columName, filterSetup, transaction: transaction);

		public Task<TResult?> MinAsync<TResult>(IDbConnection connection, string columnName, IDbTransaction transaction = null) where TResult : struct
			=> _entity.MinAsync<TResult>(connection, columnName, transaction: transaction);

		public Task<TResult?> MinAsync<TResult>(string columName, Action<TFilter> filterSetup) where TResult : struct
			=> _entity.MinAsync<TResult>(columName, filterSetup);

		public Task<TResult?> MinAsync<TResult>(string columnName) where TResult : struct
			=> _entity.MinAsync<TResult>(columnName);

		public Task<(int Deleted, int Added)> ReplaceAsync(IDbConnection conn, IDbTransaction tran, Action<TFilter> filterSetup, IEnumerable<TProjection> models)
			=> _entity.ReplaceAsync(conn, tran, filterSetup, models.Select(x => x.Model));

		public Task<(int Deleted, int Added)> ReplaceAsync(Action<TFilter> filterSetup, IEnumerable<TProjection> models)
			=> _entity.ReplaceAsync(filterSetup, models.Select(x => x.Model));

		public Task<int> UpdateAsync(IDbConnection connection, Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null, IDbTransaction transaction = null)
			=> _entity.UpdateAsync(connection, updateSetup, filterSetup, optionsSetup, tran: transaction);

		public Task<int> UpdateAsync(Action<TUpdate> updateSetup, Action<TFilter> filterSetup = null, Action<TOptions> optionsSetup = null)
			=> _entity.UpdateAsync(updateSetup, filterSetup, optionsSetup);

		public Task<int> UpdateAsync(TProjection model, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null)
			=> _entity.UpdateAsync(model.Model, OutputMapRedirect(new[] { model }, outputMap));

		public Task<int> UpdateAsync(IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null)
			=> _entity.UpdateAsync(models.Select(x => x.Model), OutputMapRedirect(models, outputMap));

		public Task<int> UpsertAsync(IDbConnection connection, IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null, IDbTransaction transaction = null)
			=> _entity.UpsertAsync(connection, models.Select(x => x.Model), outputMap: OutputMapRedirect(models, outputMap), transaction: transaction);

		public Task<int> UpsertAsync(TProjection model, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null)
			=> _entity.UpsertAsync(model.Model, OutputMapRedirect(new[] { model }, outputMap));

		public Task<int> UpsertAsync(IEnumerable<TProjection> models, Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap = null)
			=> _entity.UpsertAsync(models.Select(x => x.Model), OutputMapRedirect(models, outputMap));

		protected Action<IEnumerable<TModel>, IEnumerable<TModel>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> OutputMapRedirect(
			IEnumerable<TProjection> entitiesOfT,
			Action<IEnumerable<TProjection>, IEnumerable<TProjection>, IEnumerable<PropertyInfo>, IEnumerable<PropertyInfo>> outputMap
		)
		{
			if (outputMap == null)
				return null;

			return (entities, results, propertiesToMatch, propertiesToMap) =>
			{
				outputMap(entitiesOfT, results.Select(Instance), propertiesToMatch, propertiesToMap);
			};
		}

		protected TProjection Instance(TModel model)
		{
			if (_creator != null)
				return _creator(model);

			if (_constructorMethod == null)
				_constructorMethod = typeof(TProjection).GetConstructor(new[] { typeof(TModel) });

			return (TProjection)_constructorMethod.Invoke(new[] { model });
		}
	}

	public class ProjectedEntity<TProjection, TModel, TFilter, TOptions>
		: ProjectedEntity<TProjection, TModel, TModel, TFilter, TOptions>, IProjectedEntity<TProjection, TModel, TFilter, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TFilter : class
		where TOptions : DbQueryOptions, new()
	{
		public ProjectedEntity(Entity<TModel, TFilter, TOptions> entity)
			: base(entity)
		{

		}

		public ProjectedEntity(Entity<TModel, TFilter, TOptions> entity, Func<TModel, TProjection> creator)
			: base(entity, creator)
		{
		}
	}

	public class ProjectedEntity<TProjection, TModel, TOptions>
		: ProjectedEntity<TProjection, TModel, TModel, TOptions>, IProjectedEntity<TProjection, TModel, TOptions>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
		where TOptions : DbQueryOptions, new()
	{
		public ProjectedEntity(Entity<TModel, TModel, TOptions> entity)
			: base(entity)
		{
		}

		public ProjectedEntity(Entity<TModel, TModel, TOptions> entity, Func<TModel, TProjection> creator)
			: base(entity, creator)
		{
		}
	}

	public class ProjectedEntity<TProjection, TModel>
		: ProjectedEntity<TProjection, TModel, DbQueryOptions>, IProjectedEntity<TProjection, TModel>
		where TProjection : class, IProject<TModel>
		where TModel : class, new()
	{
		public ProjectedEntity(Entity<TModel> entity)
			: base(entity)
		{
		}

		public ProjectedEntity(Entity<TModel> entity, Func<TModel, TProjection> creator)
			: base(entity, creator)
		{
		}
	}
}
