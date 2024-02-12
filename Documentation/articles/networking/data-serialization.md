# Data Serialization

Data serialization and deserialization is done through the DataWriter and DataReader class.

## DataWriter

The data writer exposes a lot of Put methods. With this you can write directly binary serializable data to the internal
stream. Check the class to see which types are natively supported.

Beside Put you can also write a placeholder for a value, which is settable afterwards. Currently only the built in
numeric types (except nint and nuint) are supported. This method returns a reference to the data which you can write
afterwards. This is useful if you want to write a value which is not known before serializing data. For example the
amount of items serialized.

## DataReader

To read data from the DataReader you use the TryGet*Type* methods. These methods return a boolean indicating whether the
read was successful or not. This is false if the internal cursor tries to read past the current boundaries. See Regions.
The actual value is returned through the out parameter.

## Regions

It is possible to declare regions in the data stream. This is used to prevent faulty data reads.
If you try to read past the current region the read will fail. Also if you dont read "far enough" the next region will
automatically set the cursor to the appropriate position.

This prevents a singular faulty read to corrupt the whole data stream. If a read fails this error is caught and the
remaining work can continue.

To use regions, use the respective EnterRegion and ExitRegion methods. Nesting of regions is supported and highly
recommended.

