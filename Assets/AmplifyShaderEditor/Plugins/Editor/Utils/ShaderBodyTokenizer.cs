// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AmplifyShaderEditor
{
	public struct TokenDesc
	{
		public string name;
		public int position;
		public int line;

		public TokenDesc( string name, int position, int line )
		{
			this.name = name;
			this.position = position;
			this.line = line;
		}
	}

	public class ShaderBodyTokenTable
	{
		private int count = 0;
		public int Count { get { return count; } }

		private LinkedList<TokenDesc> tokens = new LinkedList<TokenDesc>();
		private Dictionary<string, List<LinkedListNode<TokenDesc>>> tokensByName = new Dictionary<string, List<LinkedListNode<TokenDesc>>>();
		private Dictionary<int, List<LinkedListNode<TokenDesc>>> tokensByLine = new Dictionary<int, List<LinkedListNode<TokenDesc>>>();			

		private static List<LinkedListNode<TokenDesc>> EmptyTokenList = new List<LinkedListNode<TokenDesc>>();

		public bool Contains( string token )
		{
			return tokensByName.ContainsKey( token );
		}

		public List<LinkedListNode<TokenDesc>> ListTokensByName( string name )
		{
			if ( tokensByName.TryGetValue( name, out List<LinkedListNode<TokenDesc>> list ) )
			{
				return list;
			}
			return EmptyTokenList;
		}
			
		public List<LinkedListNode<TokenDesc>> ListTokensByLine( int line )
		{
			if ( tokensByLine.TryGetValue( line, out List<LinkedListNode<TokenDesc>> list ) )
			{
				return list;
			}
			return EmptyTokenList;
		}

		public void Add( string name, int position, int line )
		{
			var node = tokens.AddLast( new TokenDesc( name, position, line ) );

			if ( !tokensByName.TryGetValue( name, out List<LinkedListNode<TokenDesc>> listPerName ) )
			{
				listPerName = new List<LinkedListNode<TokenDesc>>();
				tokensByName.Add( name, listPerName );
			}

			if ( !tokensByLine.TryGetValue( line, out List<LinkedListNode<TokenDesc>> listPerLine ) )
			{
				listPerLine = new List<LinkedListNode<TokenDesc>>();
				tokensByLine.Add( line, listPerLine );
			}

			listPerName.Add( node );
			listPerLine.Add( node );
			count++;
		}
	}

	public class ShaderBodyTokenizer
	{
		private static double TimeSinceStartup
		{
		#if UNITY_2020_2_OR_NEWER
			get { return Time.realtimeSinceStartupAsDouble; }
		#else
			get { return Time.realtimeSinceStartup; }
		#endif
		}

		public static ShaderBodyTokenTable Process( string body )
		{
			var tokens = new ShaderBodyTokenTable();
			int charIndex = 0;
			int charCount = body.Length;
			int line = 0;
			var tokenBuilder = new StringBuilder( 1024 );
			do
			{
				char c = body[ charIndex++ ];
				bool isBreak = ( c == '\n' );
				bool isEmpty = ( isBreak || c == ' ' || c == '\t' || c == '\r' || c == '(' || c == ')' || c == '{' || c == '}' || c == '[' || c == ']' || c == ';' || c == ',' || c == '\"' );
				if ( !isEmpty )
				{
					tokenBuilder.Clear();
					int position = charIndex;

					while ( !isEmpty && charIndex < charCount )
					{
						tokenBuilder.Append( c );
						c = body[ charIndex++ ];
						isBreak = ( c == '\n' );
						isEmpty = ( isBreak || c == ' ' || c == '\t' || c == '\r' || c == '(' || c == ')' || c == '{' || c == '}' || c == '[' || c == ']' || c == ';' || c == ',' || c == '\"' );
						line += isBreak ? 1 : 0;
					}

					string token = tokenBuilder.ToString();
					if ( !token.StartsWith( "//" ) )
					{
						tokens.Add( token, position, line );
					}
				}
				else
				{
					line += isBreak ? 1 : 0;
				}
			} while ( charIndex < charCount );
			return tokens;
		}

		public static void TestProcess( string body )
		{
			UnityEngine.Profiling.Profiler.BeginSample( "Tokenize" );
			double start = TimeSinceStartup;
			ShaderBodyTokenTable tokens = ShaderBodyTokenizer.Process( body );
			UnityEngine.Profiling.Profiler.EndSample();
			Debug.Log( "Found " + tokens.Count + " tokens, taking " + ( ( TimeSinceStartup - start ) * 1000 ) + " ms" );
			Debug.Log( "Has Fallback " + tokens.Contains( "Fallback" ) );
			Debug.Log( "Has CustomEditor " + tokens.Contains( "CustomEditor" ) );
			var list = tokens.ListTokensByName( "CustomEditor" );
			foreach ( var node in list )
			{
				Debug.Log( "Name: " + node.Value.name + ", Position: " + node.Value.position + ", Line: " + node.Value.line );
			}

			//foreach ( var node in list )
			//
			//File.WriteAllLines( "C:/Users/Diogo/Desktop/dump.txt", tokens.Keys );
		}
	}	
}
