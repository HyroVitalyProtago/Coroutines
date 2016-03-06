//-----------------------------------------------------------------------------
// Copyright(c) 2016 Jean Simonet - http://jeansimonet.com
// Distributed under the MIT License - https://opensource.org/licenses/MIT
//-----------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Coroutines
{
	namespace Diagnostics
	{
		public class Utilities
		{
			/// <summary>
			/// Utility Method that attempts to retrieve the original reflection info for an iterator,
			/// given the run-time auto-generated IEnumerator-derived object.
			/// </summary>
			public static MethodInfo FindMethod<IterationType>(IEnumerator<IterationType> stack)
			{
				MethodInfo ret = null;

				// We start by getting the actual type of the enumerator.
				// Since the type was auto-generated, the original method name is embedded in its name
				System.Type enumType = stack.GetType();

				// Start by finding the enumeration method name
				Regex regEx = new Regex(@"\<(.+)\>");
				var match = regEx.Match(enumType.Name);
				if (match.Success)
				{
					// We've got the method name, try to look it up in the reflection data of the original type
					var methodName = match.Groups[1].Value;
					var methods = enumType.DeclaringType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance);

					// The method name in the reflection data may have namespace or parameters or other decorations, so we grab all the methods that contain the name in them
					var potentialMethods = methods.Where(m => m.Name.Contains(methodName));
					if (potentialMethods.Count() > 1)
					{
						// There are multiple methods named the same, check the parameters

						// Grab the list of parameters from the auto-generated enumerable object
						// Those are all the members that don't have auto-generated names
						var enumParams = new List<KeyValuePair<string, System.Type>>();
						foreach (var member in enumType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
						{
							if (!member.Name.Contains('<') && !member.Name.Contains('$'))
							{
								enumParams.Add(new KeyValuePair<string, System.Type>(member.Name, member.FieldType));
							}
							// Else skip this member, it was auto generated
						}

						// Now iterate through the methods and compare their parameters with the enumerator object's
						foreach (var method in potentialMethods)
						{
							// Build a list of the method arguments
							var methodParams = new List<KeyValuePair<string, System.Type>>(method.GetParameters().Select(argument => new KeyValuePair<string, System.Type>(argument.Name, argument.ParameterType)));

							// Compare sizes first
							bool matching = methodParams.Count == enumParams.Count;
							if (matching)
							{
								// So far they 'could' match, make sure all the members of the enumerator object are parameters of the method
								matching = enumParams.All(kv => methodParams.Contains(kv));
							}

							if (matching)
							{
								ret = method;
								break;
							}
						}
					}
					else
					{
						// There was only one method (or none) with that name, return it
						ret = potentialMethods.FirstOrDefault();
					}
				}

				return ret;
			}
		}

		/// <summary>
		/// Helper class that will load assembly information using CECIL and keep it around until no one needs the info anymore
		/// </summary>
		public class AssemblyCache
			: System.IDisposable
		{
			// This keeps track of the assemblies that have been loaded for a given dll path
			static Dictionary<string, AssemblyDefinition> _Assemblies;

			// This maps from reflection type to path-to-the-assembly-that-defines-the-type
			static Dictionary<System.Type, string> _TypePaths;

			// This counts the number of caches open, so we know to get rid of the assembly data once it reaches 0
			static int _Counter;

			/// <summary>
			/// Static constructor, initializes the global maps
			/// </summary>
			static AssemblyCache()
			{
				_Assemblies = new Dictionary<string, AssemblyDefinition>();
				_TypePaths = new Dictionary<System.Type, string>();
				_Counter = 0;
			}

			/// <summary>
			/// Empties the maps when no one needs them anymore
			/// </summary>
			static void ClearCache()
			{
				_Assemblies.Clear();
				_TypePaths.Clear();
			}

			/// <summary>
			/// Default constructor
			/// Always use 'using(var cache = new AssemblyCache())' !!!
			/// </summary>
			public AssemblyCache()
			{
				AssemblyCache._Counter += 1;
			}

			/// <summary>
			/// Always use 'using(var cache = new AssemblyCache())' !!!
			/// </summary>
			public void Dispose()
			{
				AssemblyCache._Counter -= 1;
				if (AssemblyCache._Counter == 0)
				{
					AssemblyCache.ClearCache();
				}
			}

			/// <summary>
			/// Fetches the assembly definition corresponding to a given assembly path
			/// </summary>
			public AssemblyDefinition GetAssembly(string path)
			{
				AssemblyDefinition ret = null;
				if (!_Assemblies.TryGetValue(path, out ret))
				{
					// Tell Cecil to read the mdb symbols, this is the only way we can get decent stack traces
					var readerParameters = new ReaderParameters { ReadSymbols = true };
					ret = AssemblyDefinition.ReadAssembly(path, readerParameters);
					_Assemblies.Add(path, ret);
				}
				return ret;
			}

			/// <summary>
			/// Fetches the assembly definition corresponding to a given reflection type
			/// </summary>
			public AssemblyDefinition GetAssembly(System.Type type)
			{
				string path = "";
				if (!_TypePaths.TryGetValue(type, out path))
				{
					path = System.Reflection.Assembly.GetAssembly(type).Location;
					_TypePaths.Add(type, path);
				}
				return GetAssembly(path);
			}

			/// <summary>
			/// Fetches the CECIL type definition for a given Reflection type
			/// (internally fetches the assembly and then the type)
			/// </summary>
			public TypeDefinition GetTypeDefinition(System.Type type)
			{
				// Cecil and the reflection system don't treat type paths (as in namespace.class.type) the same way, so fix that
				string enumTypeFullname = type.FullName.Replace('+', '/');
				return GetAssembly(type).MainModule.GetType(enumTypeFullname);
			}
		}

		/// <summary>
		/// Custom stack frame information, so we can write our own stack trace for exceptions originating from the graph
		/// </summary>
		class Frame
			: System.Diagnostics.StackFrame
		{
			enum FrameType
			{
				Unknown = -1,
				Enumerable = 0,
				ParsedFrame,
			}
			FrameType _Type;
			MethodBase _Method;
			string _MethodName;
			string _MethodFullName;
			string _FileName;
			int _FileLineNumber;
			int _ILOffset;
			int _ProgramCounter;

			/// <summary>
			/// Default constructor, makes an invalid frame
			/// </summary>
			public Frame()
			{
				_Method = null;
				_MethodName = "<unknown>";
				_MethodFullName = "<unknown>";
				_FileName = "<unknown>";
				_FileLineNumber = -1;
				_ILOffset = OFFSET_UNKNOWN;
				_ProgramCounter = -1;
				_Type = FrameType.Unknown;
			}

			/// <summary>
			/// Parsed frame information constructor
			/// Used when needing to store the stack trace gotten from a standard exception
			/// </summary>
			public Frame(string methodName, string methodFullName, string fileName, int line, int ilOffset)
			{
				_Type = Frame.FrameType.ParsedFrame;
				_MethodName = methodName;
				_MethodFullName = methodFullName;
				_FileName = fileName;
				_FileLineNumber = line;
				_ILOffset = ilOffset;
			}

			/// <summary>
			/// Iterator-based constructor, but overriding the file line number
			/// </summary>
			public static Frame MakeFrame(Frame baseFrame, string filename, int fileLineNumber)
			{
				var ret = new Frame();
				ret._Method = baseFrame._Method;
				ret._MethodName = baseFrame.MethodName;
				ret._MethodFullName = baseFrame._MethodFullName;
				ret._ILOffset = baseFrame._ILOffset;
				ret._ProgramCounter = baseFrame._ProgramCounter;
				ret._Type = baseFrame._Type;

				ret._FileName = filename;
				ret._FileLineNumber = fileLineNumber;
				return ret;
			}

			/// <summary>
			/// Iterator-based constructor
			/// Used to extract out the original method name from auto-generated iterator class
			/// </summary>
			public static Frame MakeFrame<IterationType>(AssemblyCache cache, IEnumerator<IterationType> stack)
			{
				var ret = new Frame();

				// Start by finding the enumeration method from the reflection type information
				System.Type enumType = stack.GetType();
				var method = Utilities.FindMethod(stack);

				// Parse it
				if (method != null)
				{
					ret._Method = method;
					ret._Type = FrameType.Enumerable;

					string paramList = string.Join(",", method.GetParameters().Select(o => string.Format("{0} {1}", o.ParameterType, o.Name)).ToArray());
					ret._MethodName = string.Format("{0}.{1}", method.ReflectedType.FullName, method.Name);
					ret._MethodFullName = string.Format("{0}.{1}({2})", method.ReflectedType.FullName, method.Name, paramList);
				}

				// Then look for the switch statement in its body characteristic of iterators
				// The switch statement is encoded inside the MoveNext() method, and the current progress
				// along the enumeration is stored in the $PC member variable
				TypeDefinition definition = cache.GetTypeDefinition(enumType);
				MethodDefinition moveNextMethod = definition.Methods.First(m => m.Name == "MoveNext");
				if (moveNextMethod.Body != null)
				{
					// Find a switch statement
					var firstSwitch = moveNextMethod.Body.Instructions.FirstOrDefault(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Switch);

					// Grab the list of potential jumps
					var instructionArrayType = typeof(Mono.Cecil.Cil.Instruction[]);
					if (firstSwitch != null && firstSwitch.Operand.GetType() == instructionArrayType)
					{
						Mono.Cecil.Cil.Instruction[] jumps = firstSwitch.Operand as Mono.Cecil.Cil.Instruction[];

						// Read the value of the $PC counter using regular reflection
						var programCounterField = enumType.GetField("$PC", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
						int pc = (int)(programCounterField.GetValue(stack));
						ret._ProgramCounter = pc;

						// Grab the current instruction from the jump table and the $PC
						Mono.Cecil.Cil.Instruction currentInstruction = null;
						if (pc >= 0 && pc < jumps.Length)
						{
							currentInstruction = jumps[pc];
						}

						// Now look backwards in the IL code for the first instruction that has source file annotations
						var instCorSymbol = currentInstruction;
						while (instCorSymbol != null && instCorSymbol.SequencePoint == null)
						{
							instCorSymbol = instCorSymbol.Previous;
						}

						// And extract out the file and line number from that annotation
						if (instCorSymbol != null)
						{
							ret._FileName = instCorSymbol.SequencePoint.Document.Url;
							ret._FileLineNumber = instCorSymbol.SequencePoint.EndLine;
							ret._ILOffset = currentInstruction.Offset;
						}
						else if (currentInstruction != null)
						{
							// We don't have file or line info, but at least we have IL offset
							ret._ILOffset = currentInstruction.Offset;
						}
						// Else leave filename and IL offset unknown
					}
					// Else it's a switch without cases? leave filename/line unknown
				}
				// Else the method doesn't have a body? leave filename/line unknown

				return ret;
			}

			/// <summary>
			/// Override file and line info. This is useful for cases where the iterator executed a finally block (because
			/// of the exception we're trying to trace) and therefore we don't have access to the $PC member (it is -1) to
			/// extract line number. Usually, the original exception has that information and we paste it into this frame
			/// using this method.
			/// </summary>
			public void SetFileInfo(string filename, int newFileLineNumber)
			{
				_FileName = filename;
				_FileLineNumber = newFileLineNumber;
			}

			public string MethodName
			{
				get { return _MethodName; }
			}

			public string MethodFullName
			{
				get { return _MethodFullName; }
			}

			public string FileName
			{
				get { return _FileName; }
			}

			public int FileLineNumber
			{
				get { return _FileLineNumber; }
			}

			public override int GetFileColumnNumber()
			{
				return -1;
			}

			public override int GetFileLineNumber()
			{
				return _FileLineNumber;
			}

			public override string GetFileName()
			{
				switch (_Type)
				{
					case FrameType.Unknown:
						return "<Unknown>";
					case FrameType.Enumerable:
						return _FileName;
					case FrameType.ParsedFrame:
						return _FileName;
					default:
						throw new System.ArgumentOutOfRangeException("Type");
				}
			}

			public override int GetILOffset()
			{
				return _ILOffset;
			}

			public override MethodBase GetMethod()
			{
				return _Method;
			}

			public override int GetNativeOffset()
			{
				return OFFSET_UNKNOWN;
			}

			public override string ToString()
			{
				// Generate the full method name
				switch (_Type)
				{
					case FrameType.Unknown:
						return "<Unknown stack frame>";
					case FrameType.Enumerable:
						return _MethodFullName + "(at " + _FileName + ":" + _FileLineNumber + ")"; ;
					case FrameType.ParsedFrame:
						return _MethodFullName + "(at " + _FileName + ":" + _FileLineNumber + ")"; ;
					default:
						throw new System.ArgumentOutOfRangeException("Type");
				}
			}
		}


		/// <summary>
		/// Stores a collection of Graph stack frames, so we can display translated stack traces when we catch exceptions
		/// </summary>
		class Trace
			: System.Diagnostics.StackTrace
		{
			public List<Frame> Frames;

			/// <summary>
			/// Default constructor
			/// </summary>
			public Trace()
			{
				Frames = new List<Frame>();
			}

			/// <summary>
			/// Initializing constructor
			/// </summary>
			/// <param name="frames"></param>
			public Trace(List<Frame> frames)
			{
				Frames = frames;
			}

			// StackTrace overrides

			/// <summary>
			/// Fetches a specific frame
			/// </summary>
			public override System.Diagnostics.StackFrame GetFrame(int index)
			{
				return Frames[index];
			}

			/// <summary>
			/// Fetches all the frames as an array
			/// </summary>
			public override System.Diagnostics.StackFrame[] GetFrames()
			{
				return Frames.ToArray();
			}

			/// <summary>
			/// Yay! Frame count!
			/// </summary>
			public override int FrameCount
			{
				get { return Frames.Count; }
			}

			/// <summary>
			/// And printy printy!
			/// </summary>
			public override string ToString()
			{
				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < Frames.Count; ++i)
				{
					builder.AppendLine(Frames[i].ToString());
				}
				return builder.ToString();
			}
		}

		/// <summary>
		/// Our custom Exception class that will translate the stack frames of the exceptionto the original iterator method,
		/// not the auto-generated IEnumerator block.
		/// </summary>
		class CoroutineException
			: System.Exception
		{
			// Our replacement stacktrace (the only one we're going to print)
			Trace _Trace;

			public static CoroutineException MakeException<IterationType>(CoroutineException innerGraphException, IEnumerator<IterationType> newFrameIterator)
			{
				using (AssemblyCache cache = new AssemblyCache())
				{
					return new CoroutineException(innerGraphException, Frame.MakeFrame(cache, newFrameIterator));
				}
			}

			public static CoroutineException MakeException<IterationType>(System.Exception baseException, IEnumerator<IterationType> newFrameIterator)
			{
				using (AssemblyCache cache = new AssemblyCache())
				{
					return new CoroutineException(baseException, Frame.MakeFrame(cache, newFrameIterator));
				}
			}

			/// <summary>
			/// Initializing constructor - parses the original exception and matches it up with the graph info passed in
			/// </summary>
			public CoroutineException(CoroutineException innerGraphException, Frame newFrame)
				: base(innerGraphException.Message)
			{
				var frames = new List<Frame>(innerGraphException.Trace.Frames);
				frames.Add(newFrame);
				_Trace = new Trace(frames);
			}

			/// <summary>
			/// Initializing constructor - parses the original exception and matches it up with the graph info passed in
			/// </summary>
			public CoroutineException(System.Exception innerException, Frame newFrame)
				: base(innerException.Message)
			{
				// Create a graph call stack
				var frames = new List<Frame>();

				if (innerException.StackTrace != null)
				{
					// Parse the origial stack trace
					Regex regEx = new Regex(@"\s*at ([^()]+)\s*\((.*)\)\s*\[0x(.+)\] in (.+):(\d+)\s*\r\n");
					var match = regEx.Match(innerException.StackTrace);
					while (match.Success)
					{
						// We parsed the info properly, Make it a new frame entry
						frames.Add(new Frame(
							match.Groups[1].Value,
							match.Groups[1].Value + "(" + match.Groups[2].Value + ")",
							match.Groups[4].Value,
							System.Convert.ToInt32(match.Groups[5].Value),
							System.Int32.Parse(match.Groups[3].Value, System.Globalization.NumberStyles.HexNumber)));

						match = match.NextMatch();
					}

					// Remove the last frame
					var lastFrame = frames[frames.Count - 1];
					frames.RemoveAt(frames.Count - 1);

					// At the site of the exception, there is no valid line number stored in the coroutine symbol data
					// But it's always the actual line info stored in the last original stack frame
					frames.Add(Frame.MakeFrame(newFrame, lastFrame.FileName, lastFrame.FileLineNumber));
				}

				_Trace = new Trace(frames);
			}

			/// <summary>
			/// Get the stack trace
			/// </summary>
			public override string StackTrace
			{
				get { return _Trace.ToString(); }
			}

			/// <summary>
			/// Grab the stack trace directly
			/// </summary>
			public Trace Trace
			{
				get { return _Trace; }
			}
		}
	}
}
