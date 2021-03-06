﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using SharpYaml.Serialization;

namespace Elastic.Configuration.FileBased.Yaml
{
	public abstract class YamlConfigurationBase<TSettings> where TSettings : class, IYamlSettings, new()
	{
		protected TSettings YamlSettings { get; } = new TSettings();
		public string Path { get; }
		public IFileSystem FileSystem { get; }
		public bool FoundButNotValid { get; }

		protected YamlConfigurationBase(string path, IFileSystem fileSystem)
		{
			this.FileSystem = fileSystem ?? new FileSystem();
			this.Path = path;
			if (string.IsNullOrEmpty(path) || !this.FileSystem.File.Exists(this.Path)) return;

			try
			{
				var rawText = this.FileSystem.File.ReadAllText(Path);

				// we flatten because neither yamldotnet nor sharpyaml can bind nested.props in all variants
				var rawYaml = this._serializer.Deserialize<Dictionary<string, object>>(rawText);
				var dict = Flatten(rawYaml);

				// these might be lists disguised as strings, fix them
				var maybeLists = new[]
				{
					"discovery.seed_hosts",
					"cluster.initial_master_nodes"
				};

				foreach (var item in maybeLists)
				{
					if (dict.TryGetValue(item, out object objValue) && objValue is string value)
					{
						dict[item] = value
							.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(v => v.Trim())
							.ToList();
					}
				}

				var flattenedYaml = this._serializer.Serialize(dict);

				this.YamlSettings = this._serializer.Deserialize<TSettings>(flattenedYaml) ?? new TSettings();
			}
			catch
			{
				this.FoundButNotValid = true;
				this.YamlSettings = new TSettings();
			}
		}

		private readonly Serializer _serializer = new Serializer(new SerializerSettings
		{
			SerializeDictionaryItemsAsMembers = true,
			EmitTags = false,
			EmitDefaultValues = false,
			EmitAlias = false
		});


		private static IDictionary<string, object> Flatten(IDictionary<string, object> parent)
		{
			var newDictionary = new Dictionary<string, object>();
			if (parent == null) return newDictionary;
			foreach (var kv in parent)
			{
				var value = kv.Value as IDictionary<object, object>;
				if (value != null)
				{
					Flatten(newDictionary, value, (string)kv.Key);
					continue;
				}
				newDictionary.Add(kv.Key, kv.Value);
			}
			return newDictionary;
		}

		private static void Flatten(IDictionary<string, object> parent, IDictionary<object, object> child, string suffix)
		{
			if (child == null) return;
			foreach (var kv in child)
			{
				var value = kv.Value as IDictionary<object, object>;
				if (value != null)
				{
					Flatten(parent, value, suffix + "." + (string)kv.Key);
					continue;
				}
				parent.Add(suffix + "." + kv.Key, kv.Value);
			}
		}

		public void Save()
		{
			using (var writer = new StringWriter())
			{
				this._serializer.Serialize(writer, this.YamlSettings);
				this.FileSystem.File.WriteAllText(this.Path, writer.ToString());
			}
		}
	}
}
