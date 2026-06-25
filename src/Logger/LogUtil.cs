using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.Drawing;

/**
 * @brief 
 * @details 
 * 사용법 :
 * EADebug.Log( LogUtil.Style( LogUtil.Array( ": ", "Step", step.ToString() ), Color.magenta, FontStyle.BoldAndItalic ) );
 * @see 
 * @author Samuel Kang
 * @warning 
 * @todo 
 */
public class LogUtil
{
	private static string defaultSeparator = "\n"; // default separator

	/// <summary>
	/// Prints all values in an array of text. Good for debugging two or more variables in the same call.
	/// </summary>
	/// <param name="separator">Separator</param>
	/// <param name="strings">Data to be printed.</param>
	public static string Array( string separator, params string[] strings )
	{
		string value = null;
		for ( int i = 0; i < strings.Length; i++ ) {
			if ( separator != null )
				value += String.Format( strings[i] + "{0}", i < strings.Length - 1 ? separator : "" );
			else
				value += String.Format( strings[i] + "{0}", i < strings.Length - 1 ? defaultSeparator : "" );
		}
		return value;
	}

	/// <summary>
	/// Prints all values in an array separated with the default separator.
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="array">Array of T to be printed.</param>
	public static string Array<T>( T[] array )
	{
		return Array<T>( null, array );
	}

	/// <summary>
	/// Prints all values in an array.
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="separator">Separator</param>
	/// <param name="array">Array of T to be printed.</param>
	public static string Array<T>( string separator, T[] array )
	{
		string value = null;
		for ( int i = 0; i < array.Length; i++ ) {
			if ( separator != null )
				value += String.Format( array[i].ToString() + "{0}", i < array.Length - 1 ? separator : "" );
			else
				value += String.Format( array[i].ToString() + "{0}", i < array.Length - 1 ? defaultSeparator : "" );
		}
		return value;
	}

	/// <summary>
	/// Prints all values in a two-dimensional array separated with the default separator.
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="array">Array of T to be printed.</param>
	public static string Array2D<T>( T[,] array )
	{
		return Array2D<T>( null, array );
	}

	/// <summary>
	/// Prints all values in a two-dimensional array.
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="separator">Separator</param>
	/// <param name="array">Array of T to be printed.</param>
	public static string Array2D<T>( string separator, T[,] array )
	{
		string value = null;
		for ( int i = 0; i < array.GetUpperBound(0); i++ ) {   
			for ( int j = 0; j < array.GetUpperBound(1); j++ ) {
				if ( separator != null ) 
					value += String.Format( array[i, 0].ToString() + ", " + array[i, 1].ToString() + "{0}", i*j < array.Length - 1 ? separator : "" );
				else
					value += String.Format( array[i, 0].ToString() + ", " + array[i, 1].ToString() + "{0}", i*j < array.Length - 1 ? defaultSeparator : "" );
			}
		}
		return value;
	}

	/// <summary>
	/// Prints all values in a three-dimensional array separated with the default separator.
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="array">Array of T to be printed.</param>
	public static string Array3D<T>( T[,,] array )
	{
		return Array3D<T>( null, array );
	}

	/// <summary>
	/// Prints all values in a three-dimensional array.
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="separator">Separator</param>
	/// <param name="array">Array of T to be printed.</param>
	public static string Array3D<T>( string separator, T[,,] array )
	{
		string value = null;
		int r = 1;
		for ( int i = 0; i < array.GetLength( 2 ); i++ ) {
			for ( int y = 0; y < array.GetLength( 1 ); y++ ) {
				for ( int x = 0; x < array.GetLength( 0 ); x++ ) {
					if ( separator != null )
						value += String.Format( r.ToString() + ". " + "[" + x.ToString() + ", " + y.ToString() + ", " + i.ToString() + "] =" + array[x, y, i] + "{0}", i * y * x < array.Length - 1 ? separator : "" );
					else
						value += String.Format( r.ToString() + ". " + "[" + x.ToString() + ", " + y.ToString() + ", " + i.ToString() + "] =" + array[x, y, i] + "{0}", i * y * x < array.Length - 1 ? defaultSeparator : "" );
					r++;
				}
			}
		}
		value += String.Format( "\narray.length = {0}", array.Length ); 
		return value;
	}

	/// <summary>
	/// Prints all values in a list separated with the default separator
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="list">List of T to be printed.</param>
	public static string List<T>( List<T> list )
	{
		return List<T>( null, list );
	}

	/// <summary>
	/// Prints all values in a list
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="separator">Separator</param>
	/// <param name="list">List of T to be printed.</param>
	public static string List<T>( string separator, List<T> list )
	{
		string value = null;
		for ( int i = 0; i < list.Count; i++ ) {
			if ( separator != null )
				value += String.Format( list[i].ToString() + "{0}", i < list.Count - 1 ? separator : "" );
			else
				value += String.Format( list[i].ToString() + "{0}", i < list.Count - 1 ? defaultSeparator : "" );
		}
		return value;
	}

	/// <summary>
	/// Prints all keys and values in a dictionary separated with the default separator
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <typeparam name="K">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="dictionary">Dictionary with key of T and values of K to be printed.</param>
	public static string Dictionary<T, K>( Dictionary<T, K> dictionary )
	{
		return Dictionary<T, K>( null, dictionary );
	}

	/// <summary>
	/// Prints all keys and values in a dictionary.
	/// </summary>
	/// <typeparam name="T">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <typeparam name="K">Generic type parameter. When you call it, it can be any type: string, int, Vector3 etc.</typeparam>
	/// <param name="separator">Separator</param>
	/// <param name="dictionary">Dictionary with key of T and values of K to be printed.</param>
	public static string Dictionary<T, K>( string separator, Dictionary<T, K> dictionary )
	{
		string value = null;
		int i = 0;
		foreach ( KeyValuePair<T, K> d in dictionary ) {
			if ( separator != null )
				value += String.Format( d.Key.ToString() + ", " + d.Value.ToString() + "{0}", i < dictionary.Count - 1 ? separator : "" );
			else
				value += String.Format( d.Key.ToString() + ", " + d.Value.ToString() + "{0}", i < dictionary.Count - 1 ? defaultSeparator : "" );

			i++;
		}
		return value;
	}

	/// <summary>
	/// Prints exception with details.
	/// </summary>
	/// <param name="e">Exception to be printed</param>
	public static string ExceptionDetails( Exception e )
	{
		string ex = "";
		ex += e.ToString();
		ex += "\nInnerException: " + e.InnerException;
		ex += "\nMessage: " + e.Message;
		ex += "\nSource: " + e.Source;
		ex += "\nStackTrace: " + e.StackTrace;
		ex += "\nTargetSite: " + e.TargetSite;
		return ex;
	}
}

public static class ColoredLogHelper
{
	/// <summary>
	/// Converts the Color to HEX code by converting the RBA values.
	/// </summary>
	/// <param name="color">The color to convert to HEX code.</param>
	/// <returns>The color in HEX code</returns>
	public static string ColorToHex( Color color )
	{
		return "#" + color.R.ToString( "X2" ) + color.G.ToString( "X2" ) + color.B.ToString( "X2" );
	}

	/// <summary>
	/// Adds a tag around the the string
	/// </summary>
	/// <param name="str">The string the function is an extension of.</param>
	/// <param name="tag">The tag (I.E: b = Bold, i = Italics).</param>
	/// <returns>Returns the new string with the tag surrounding it.</returns>
	public static string AddTag( this string str, string tag )
	{
		return "<" + tag + ">" + str + "</" + tag + ">";
	}

	/// <summary>
	/// Adds a color tag around the string with a specific HEX code.
	/// </summary>
	/// <param name="str">The string the function is an extension of.</param>
	/// <param name="colorInHex">The HEX code which was converted from a color.</param>
	/// <returns>Returns the new string with the color tag surrounding it</returns>
	public static string AddColorTag( this string str, string colorInHex )
	{
		return "<color=" + colorInHex + ">" + str + "</color>";
	}

	/// <summary>
	/// Adds a color tag around the string with a specific HEX code.
	/// Uses the Unity Color and converts it into HEX code.
	/// </summary>
	/// <param name="str">The string the function is an extension of.</param>
	/// <param name="color">The Unity Color</param>
	/// <returns>Returns the new string with the color tag surrounding it.</returns>
	public static string AddColorTag( this string str, Color color )
	{
		string colorInHex = ColoredLogHelper.ColorToHex( color );
		return str.AddColorTag( colorInHex );
	}

	/// <summary>
	/// Adds a color tag around the key phraase within the message with a specific HEX code.
	/// Uses the Unity Color and converts it into HEX code.
	/// </summary>
	/// <param name="str">The string the function is an extension of.</param>
	/// <param name="keyPhrase">The key phrase to stylize within the message.</param>
	/// <param name="colorInHex">The HEX code which was converted from a color.</param>
	/// <param name="bold">Make the key phrase bold?</param>
	/// <param name="italic">Make the key phrase italic?</param>
	/// <returns>Returns the new string with the color tag surrounding it.</returns>
	public static string AddColorToKeyPhrase( this string str, string keyPhrase, string colorInHex, bool bold, bool italic )
	{
		if ( keyPhrase == string.Empty || !str.ToLower().Contains( keyPhrase.ToLower() ) ) { return str; }

		string message = keyPhrase.AddColorTag( colorInHex );

		if ( bold ) { message = message.AddTag( "b" ); }
		if ( italic ) { message = message.AddTag( "i" ); }

		message = str.Replace( keyPhrase, message );
		return message;
	}

	/// <summary>
	/// Adds a color tag around the key phraase within the message with a specific HEX code.
	/// Uses the Unity Color and converts it into HEX code.
	/// </summary>
	/// <param name="str">The string the function is an extension of.</param>
	/// <param name="keyPhrase">The key phrase to stylize within the message.</param>
	/// <param name="color">The Unity Color</param>
	/// <param name="bold">Make the key phrase bold?</param>
	/// <param name="italic">Make the key phrase italic?</param>
	/// <returns>Returns the new string with the color tag surrounding it.</returns>
	public static string AddColorToKeyPhrase( this string str, string keyPhrase, Color color, bool bold, bool italic )
	{
		string colorInHex = ColoredLogHelper.ColorToHex(color);
		
		return str.AddColorToKeyPhrase( keyPhrase, colorInHex, bold, italic );
	}

	/// <summary>
	/// Checks whether a string is fully empty or not.
	/// </summary>
	/// <param name="str">The string the function is an extension of.</param>
	/// <returns>Returns whether a string has all spaces or not</returns>
	public static bool IsCompletelyEmpty( this string str )
	{
		foreach ( char character in str ) {
			if ( character != ' ' )
				return false;
		}
		return true;
	}

	public static string UTF8ByteArrayToString( byte[] characters )
	{
		UTF8Encoding encoding = new UTF8Encoding();
		string constructedString = encoding.GetString( characters );
		return (constructedString);
	}

	public static byte[] StringToUTF8ByteArray( string pXmlString )
	{
		UTF8Encoding encoding = new UTF8Encoding();
		byte[] byteArray = encoding.GetBytes( pXmlString );
		return byteArray;
	}

	

}
