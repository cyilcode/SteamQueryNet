using SteamQueryNet.Attributes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SteamQueryNet.Utils
{
	internal sealed class DataResolutionUtils
	{
		internal const byte RESPONSE_HEADER_COUNT = 5;
		internal const byte RESPONSE_CODE_INDEX = 5;

		internal static IEnumerable<byte> ExtractData<TObject>(
			TObject objectRef,
			byte[] dataSource,
			string edfPropName = "",
			bool stripHeaders = false)
				where TObject : class
		{
			IEnumerable<byte> takenBytes = new List<byte>();

			// We can be a good guy and ask for any extra jobs :)
			IEnumerable<byte> enumerableSource = stripHeaders
				? dataSource.Skip(RESPONSE_HEADER_COUNT)
				: dataSource;

			// We get every property that does not contain ParseCustom and NotParsable attributes on them to iterate through all and parse/assign their values.
			IEnumerable<PropertyInfo> propsOfObject = typeof(TObject).GetProperties()
				.Where(x => !x.CustomAttributes.Any(y => y.AttributeType == typeof(ParseCustomAttribute) || y.AttributeType == typeof(NotParsableAttribute)));

			foreach (PropertyInfo property in propsOfObject)
			{
				/* Check for EDF property name, if it was provided then it mean that we have EDF properties in the model.
				 * You can check here: https://developer.valvesoftware.com/wiki/Server_queries#A2S_INFO to get more info about EDF's. */
				if (!string.IsNullOrEmpty(edfPropName))
				{
					// Does the property have an EDFAttribute assigned ?
					CustomAttributeData edfInfo = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(EDFAttribute));
					if (edfInfo != null)
					{
						// Get the EDF value that was returned by the server.
						byte edfValue = (byte)typeof(TObject).GetProperty(edfPropName).GetValue(objectRef);

						// Get the EDF condition value that was provided in the model.
						byte edfPropertyConditionValue = (byte)edfInfo.ConstructorArguments[0].Value;

						// Continue if the condition does not pass because it means that the server did not include any information about this property.
						if ((edfValue & edfPropertyConditionValue) <= 0) { continue; }
					}
				}

				/* Basic explanation of what is going of from here;
				 * Get the type of the property and get amount of bytes of its size from the response array,
				 * Convert the parsed value to its type and assign it.
				 */

				/* We have to handle strings separately since their size is unknown and they are also null terminated.
				 * Check here: https://developer.valvesoftware.com/wiki/String for further information about Strings in the protocol.
				 */
				if (property.PropertyType == typeof(string))
				{
					// Clear the buffer first then take till the termination.
					takenBytes = enumerableSource.TakeWhile(x => x != 0);

					// Parse it into a string.
					property.SetValue(objectRef, Encoding.UTF8.GetString(takenBytes.ToArray()));

					// Update the source by skipping the amount of bytes taken from the source and + 1 for termination byte.
					enumerableSource = enumerableSource.Skip(takenBytes.Count() + 1);
				}
				else
				{
					// Is the property an Enum ? if yes we should be getting the underlying type since it might differ.
					Type typeOfProperty = property.PropertyType.IsEnum
						? property.PropertyType.GetEnumUnderlyingType()
						: property.PropertyType;

					// Extract the value and the size from the source.
					(object result, int size) = ExtractMarshalType(enumerableSource, typeOfProperty);

					/* If the property is an enum we should parse it first then assign its value,
					 * if not we can just give it to SetValue since it was converted by ExtractMarshalType already.*/
					property.SetValue(objectRef, property.PropertyType.IsEnum
						? Enum.Parse(property.PropertyType, result.ToString())
						: result);

					// Update the source by skipping the amount of bytes taken from the source.
					enumerableSource = enumerableSource.Skip(size);
				}
			}

			// We return the last state of the processed source.
			return enumerableSource;
		}

		internal static List<TObject> ExtractPlayersData<TObject>(byte[] rawSource)
			where TObject : class
		{
			// Create a list to contain the serialized data.
			var objectList = new List<TObject>();

			// Skip the response headers.
			byte itemCount = rawSource[RESPONSE_CODE_INDEX];

			// Skip +1 for item_count
			IEnumerable<byte> dataSource = rawSource.Skip(RESPONSE_HEADER_COUNT + sizeof(byte));

			for (byte i = 0; i < itemCount; i++)
			{
				// Activate a new instance of the object.
				var objectInstance = Activator.CreateInstance<TObject>();

				// Extract the data.
				dataSource = ExtractData(objectInstance, dataSource.ToArray());

				// Add it into the list.
				objectList.Add(objectInstance);
			}

			return objectList;
		}

		internal static List<TObject> ExtractRulesData<TObject>(byte[] rawSource)
			where TObject : class
		{
			// Create a list to contain the serialized data.
			var objectList = new List<TObject>();

			// Skip the response headers.
			Int16 itemCount = BitConverter.ToInt16(
				rawSource
					.Skip(RESPONSE_CODE_INDEX)
					.Take(sizeof(Int16))
					.ToArray()
				);

			// Skip +2 for item_count, because its short
			IEnumerable<byte> dataSource = rawSource.Skip(RESPONSE_HEADER_COUNT + sizeof(Int16));

			for (byte i = 0; i < itemCount; i++)
			{
				// Activate a new instance of the object.
				var objectInstance = Activator.CreateInstance<TObject>();

				// Extract the data.
				dataSource = ExtractData(objectInstance, dataSource.ToArray());

				// Add it into the list.
				objectList.Add(objectInstance);
			}

			return objectList;
		}

		internal static (object, int) ExtractMarshalType(IEnumerable<byte> source, Type type)
		{
			// Get the size of the given type.
			int sizeOfType = Marshal.SizeOf(type);

			// Take amount of bytes from the source array.
			IEnumerable<byte> takenBytes = source.Take(sizeOfType);

			// We actually need to go into an unsafe block here since as far as i know, this is the only way to convert a byte[] source into its given type on runtime.
			unsafe
			{
				fixed (byte* sourcePtr = takenBytes.ToArray())
				{
					return (Marshal.PtrToStructure(new IntPtr(sourcePtr), type), sizeOfType);
				}
			}
		}
	}
}
