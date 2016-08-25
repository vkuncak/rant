﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Rant.Core.IO;
using Rant.Core.Stringes;

namespace Rant.Core.Compiler.Syntax
{
	/// <summary>
	/// Represents a Rant Syntax Tree (RST) node for a Rant pattern. This is the base class for all Rant actions.
	/// </summary>
	internal abstract class RST
	{
		private const uint NullRST = 0x4e554c4c;
		private static readonly Dictionary<uint, Type> _rstTypeMap = new Dictionary<uint, Type>();
		private static readonly Dictionary<Type, uint> _rstIDMap = new Dictionary<Type, uint>(); 

		static RST()
		{
			foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
				.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(RST))))
			{
				var attr = type.GetCustomAttribute<RSTAttribute>();
				if (attr == null) continue;
				_rstTypeMap[attr.TypeCode] = type;
				_rstIDMap[type] = attr.TypeCode;
			}
		}

		public static void SerializeRST(RST rst, EasyWriter output)
		{
			var stack = new Stack<IEnumerator<RST>>(10);
			stack.Push(rst.SerializeObject(output));
			top:
			while (stack.Count > 0)
			{
				var serializer = stack.Peek();

				while (serializer.MoveNext())
				{
					if (serializer.Current == null)
					{
						output.Write(NullRST);
					}
					else
					{
						stack.Push(serializer.Current.Serialize(output));
						goto top;
					}
				}

				stack.Pop();
			}
		}

		public static RST DeserializeRST(EasyReader input)
		{
			Type rootType;
			if (!_rstTypeMap.TryGetValue(input.ReadUInt32(), out rootType) || rootType != typeof(RstSequence))
				throw new InvalidDataException("Top-level RST must be a sequence.");

			int rootLine = input.ReadInt32();
			int rootCol = input.ReadInt32();
			int rootIndex = input.ReadInt32();
			var rootRST = Activator.CreateInstance(rootType, new TokenLocation(rootLine, rootCol, rootIndex)) as RstSequence;
			if (rootRST == null) throw new InvalidDataException("Failed to create top-level RST.");

			var stack = new Stack<IEnumerator<DeserializeRequest>>(10);
			stack.Push(rootRST.Deserialize(input));
			top:
			while (stack.Count > 0)
			{
				var deserializer = stack.Peek();

				while (deserializer.MoveNext())
				{
					if (deserializer.Current.TypeCode == NullRST)
					{
						deserializer.Current.SetResult(null);
						continue;
					}

					Type type;
					if (!_rstTypeMap.TryGetValue(deserializer.Current.TypeCode, out type))
						throw new InvalidDataException($"Invalid RST type code: {deserializer.Current.TypeCode:X8}");

					int line = input.ReadInt32();
					int col = input.ReadInt32();
					int index = input.ReadInt32();
					var rst = Activator.CreateInstance(type, new TokenLocation(line, col, index)) as RST;
					if (rst == null) throw new InvalidDataException($"Failed to create RST of type '{type.Name}'.");
					deserializer.Current.SetResult(rst);
					goto top;
				}

				stack.Pop();
			}

			return rootRST;
		}

		internal TokenLocation Location;

		protected RST(Stringe location)
		{
			if (location == null) throw new ArgumentNullException(nameof(location));
			Location = TokenLocation.FromStringe(location);
		}

		protected RST(TokenLocation location)
		{
			Location = location;
		}

		/// <summary>
		/// Performs the operations defined in the RST, given a specific sandbox to operate upon.
		/// </summary>
		/// <param name="sb">The sandbox on which to operate.</param>
		/// <returns></returns>
		public abstract IEnumerator<RST> Run(Sandbox sb);

		private IEnumerator<RST> SerializeObject(EasyWriter output)
		{
			output.Write(_rstIDMap[GetType()]);
			output.Write(Location.Line);
			output.Write(Location.Column);
			output.Write(Location.Index);
			return Serialize(output);
		}

		private IEnumerator<DeserializeRequest> DeserializeObject(EasyReader input)
		{
			// Type code and location have already been read
			return Deserialize(input);
		}

		protected abstract IEnumerator<RST> Serialize(EasyWriter output);

		protected abstract IEnumerator<DeserializeRequest> Deserialize(EasyReader input);
	}
}