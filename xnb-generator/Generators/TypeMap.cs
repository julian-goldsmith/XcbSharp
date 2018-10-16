﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace xnbgenerator.Generators.Types
{
    public class XType
	{
		public string Name;
		public string CSName;
		public XType Type;

		int? _size;
		public int Size
		{
			set
			{
				_size = value;
			}

            get
			{
				if (_size.HasValue)
				{
					return _size.Value;
				}
				else if (Type != null)
				{
					return Type.Size;
				}
				else
				{
					throw new InvalidOperationException("Type has no size: " + Name);
				}
			}
		}
	}

	public class TypeMap : Dictionary<string, XType>
    {
		public TypeMap()
		{
			Add("sbyte", new XType { Name = "sbyte", CSName = "sbyte", Size = 1, });
			Add("byte", new XType { Name = "byte", CSName = "byte", Size = 1, });
			Add("bool", new XType { Name = "bool", CSName = "bool", Size = 1, });

			Add("short", new XType { Name = "short", CSName = "short", Size = 2, });
			Add("ushort", new XType { Name = "ushort", CSName = "ushort", Size = 2, });
			Add("char", new XType { Name = "char", CSName = "char", Size = 2, });
			Add("uchar", new XType { Name = "uchar", CSName = "uchar", Size = 2, });

			Add("int", new XType { Name = "int", CSName = "int", Size = 4, });
			Add("uint", new XType { Name = "uint", CSName = "uint", Size = 4, });

			Add("long", new XType { Name = "long", CSName = "long", Size = 8, });
			Add("ulong", new XType { Name = "ulong", CSName = "ulong", Size = 8, });
            
            Add("Id", new XType { Name = "Id", CSName = "Id", Type = this["uint"], });
			Add("string", new XType { Name = "string", CSName = "string", Size = -1, });        // FIXME
            Add("void", new XType { Name = "void", CSName = "IntPtr", Size = -1, });            // FIXME
		}

		public void Load(string fname)
        {
            char[] delim = { '\t' };
            
			using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read))
			using (StreamReader sr = new StreamReader(fs))
			{            
				string ln;
				while ((ln = sr.ReadLine()) != null)
				{
					if (string.IsNullOrWhiteSpace(ln) || ln.StartsWith("#"))
					{
						continue;
					}

					string[] parts = ln.Split(delim);
					if (parts.Length != 2)
					{
						Console.Error.WriteLine("Error: Bad type map file: " + fname);
						continue;
					}

					if (!ContainsKey(parts[1].Trim()))
					{
						Console.Error.WriteLine("Couldn't get base type {0}", parts[1].Trim());
						continue;
					}
                    
					XType type = new XType
					{
						Name = parts[0].Trim(),
						CSName = this[parts[1].Trim()].CSName,
						Type = this[parts[1].Trim()],
					};

					this[type.Name] = type;
				}
			}
        }

        public void Save(string fname)
        {
			using (StreamWriter sw = new StreamWriter(new FileStream(fname, FileMode.Create, FileAccess.Write)))
			{
				sw.WriteLine("#TypeMap for " + "[]");
				sw.WriteLine("#Generated by xnb-generator");
				sw.WriteLine();

				foreach (KeyValuePair<string, XType> entry in this)
				{
					sw.WriteLine(entry.Key + "\t" + entry.Value.CSName);
				}
			}
        }

		public string TypeToCs(string name)
        {
			if (ContainsKey(name))
			{
				return this[name].CSName;
			}

            Console.Error.WriteLine("Warning: typeMap doesn't contain " + name);
            return GeneratorUtil.ToCs(name);
        }
        
        public string NewTypeToCs(string name)
        {
            return NewTypeToCs(name, "");
        }

        public string NewTypeToCs(string name, string suffix)
        {
            if (ContainsKey(name))
            {
				string cs = this[name].Name;

                if (cs.ToLower() == name.ToLower())
                {
					// this type is already defined as a primitive
					Console.Error.WriteLine("Warning: type \"{0}\" already defined as a primitive", name);
                    return NewTypeToCs(name + "_fake");
                }
				else
				{
					return this[name].Type.CSName;
				}
            }
            else
            {            
				XType type = new XType
				{
					Name = name,
					CSName = GeneratorUtil.ToCs(name) + suffix,
				};

                if (suffix == "Id")
				{
					type.Type = this["uint"];
				}

				this[type.Name] = type;

				return type.CSName;
            }
		}

        public int SizeOfType(string name)
        {
			if (ContainsKey(name))
			{
				XType type = this[name];

				try
				{
					return type.Size;
				}
				catch
				{
					// FIXME
					return 0;               
				}
			}
			else
			{
				Console.Error.WriteLine("Error: Size not known for type: " + name);
				return 0;
			}
        }
    }
}