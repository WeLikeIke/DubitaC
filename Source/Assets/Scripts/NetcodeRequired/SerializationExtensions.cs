using Unity.Netcode;


/// <summary>
/// The <see cref="SerializationExtensions"/> class is invoked internally by the <see cref="Unity.Netcode"/> Rpc serializer.
/// Without this, arrays of reference types (like strings) would fail.
/// For more complex types, one would need to extend the ReadValueSafe and WriteValueSafe methods for the single
/// data, and then extend it again for a collection (like arrays), strings are a special case, as the base implementation offers a
/// valid serialization for a single refernce type only: strings.
/// </summary>
public static class SerializationExtensions {

    /// <summary>
    /// Extension method for the <see cref="FastBufferReader"/> struct in <see cref="Unity.Netcode"/>.
    /// This overload will fill a string array with the contents of the reader buffer.
    /// Notice how we call the ReadValueSafe overload that works on strings for each cell.
    /// </summary>
    /// <param name="reader">The <see cref="FastBufferReader"/> to call the method from.</param>
    /// <param name="value">The final returned value, in this case a string array.</param>
    public static void ReadValueSafe(this FastBufferReader reader, out string[] value) {

        //First we need to read the length of the array from the buffer
        reader.ReadValueSafe(out int length);

        //Create an output array to be returned
        value = new string[length];

        //Read the contents of the array from the buffer one by one
        for (var i = 0; i < length; ++i) {

            //Store the read value in the output array at i's index
            reader.ReadValueSafe(out value[i]);
        }
    }

    /// <summary>
    /// Extension method for the <see cref="FastBufferReader"/> struct in <see cref="Unity.Netcode"/>.
    /// This overload will fill a the buffer of the writer with the contents of the given array.
    /// Notice how we call the WriteValueSafe overload that works on strings for each cell.
    /// </summary>
    /// <param name="reader">The <see cref="FastBufferWriter"/> to call the method from.</param>
    /// <param name="value">The give value, in this case a string array.</param>
    public static void WriteValueSafe(this FastBufferWriter writer, in string[] value) {

        //First we need to write the length of the array to the buffer
        writer.WriteValueSafe(value.Length);

        //Write the contents of the array to the buffer one by one
        foreach (var item in value) {
            writer.WriteValueSafe(item);
        }
    }
}
