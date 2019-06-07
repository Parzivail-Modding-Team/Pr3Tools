using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace Fbx2Pr3
{
    internal class ObjectDumper
    {
        public static void Write(object element)
        {
            Write(element, 0);
        }

        public static void Write(object element, int depth)
        {
            Write(element, depth, Console.Out);
        }

        public static void Write(object element, int depth, TextWriter log)
        {
            var dumper = new ObjectDumper(depth) { _writer = log };
            dumper.WriteObject(null, element);
        }

        private TextWriter _writer;
        private int _pos;
        private int _level;
        private readonly int _depth;

        private ObjectDumper(int depth)
        {
            _depth = depth;
        }

        private void Write(string s)
        {
            if (s == null) return;
            _writer.Write(s);
            _pos += s.Length;
        }

        private void WriteIndent()
        {
            for (var i = 0; i < _level; i++) _writer.Write("  ");
        }

        private void WriteLine()
        {
            _writer.WriteLine();
            _pos = 0;
        }

        private void WriteTab()
        {
            Write("\n  ");
            while (_pos % 8 != 0) Write(" ");
        }

        private void WriteObject(string prefix, object element)
        {
            switch (element)
            {
                case null:
                case ValueType _:
                case string _:
                    WriteIndent();
                    Write(prefix);
                    WriteValue(element);
                    WriteLine();
                    break;
                case IEnumerable enumerableElement:
                    {
                        foreach (var item in enumerableElement)
                        {
                            if (item is IEnumerable && !(item is string))
                            {
                                WriteIndent();
                                Write(prefix);
                                Write("...");
                                WriteLine();
                                if (_level >= _depth) continue;
                                _level++;
                                WriteObject(prefix, item);
                                _level--;
                            }
                            else
                            {
                                WriteObject(prefix, item);
                            }
                        }

                        break;
                    }
                default:
                    {
                        var members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                        WriteIndent();
                        Write(prefix);
                        var propWritten = false;
                        foreach (var m in members)
                        {
                            var f = m as FieldInfo;
                            var p = m as PropertyInfo;
                            if (f == null && p == null) continue;
                            if (propWritten)
                            {
                                WriteTab();
                            }
                            else
                            {
                                propWritten = true;
                            }
                            Write(m.Name);
                            Write("=");
                            var t = f != null ? f.FieldType : p.PropertyType;
                            if (t.IsValueType || t == typeof(string))
                            {
                                WriteValue(f != null ? f.GetValue(element) : p.GetValue(element, null));
                            }
                            else
                            {
                                Write(typeof(IEnumerable).IsAssignableFrom(t) ? "..." : "{ }");
                            }
                        }
                        if (propWritten) WriteLine();
                        if (_level < _depth)
                        {
                            foreach (var m in members)
                            {
                                var f = m as FieldInfo;
                                var p = m as PropertyInfo;
                                if (f == null && p == null) continue;

                                var t = f != null ? f.FieldType : p.PropertyType;
                                if (t.IsValueType || t == typeof(string)) continue;

                                var value = f != null ? f.GetValue(element) : p.GetValue(element, null);
                                if (value == null) continue;

                                _level++;
                                WriteObject(m.Name + ": ", value);
                                _level--;
                            }
                        }

                        break;
                    }
            }
        }

        private void WriteValue(object o)
        {
            switch (o)
            {
                case null:
                    Write("null");
                    break;
                case DateTime time:
                    Write(time.ToShortDateString());
                    break;
                case ValueType _:
                case string _:
                    Write(o.ToString());
                    break;
                case IEnumerable _:
                    Write("...");
                    break;
                default:
                    Write("{ }");
                    break;
            }
        }
    }
}
