using System;

namespace Pr3Tools
{
/**
 * Accepts various forms of a floating point half-precision (2 byte) number
 * and contains methods to convert to a
 * full-precision floating point number Float and Double instance.
 * <p>
 * This implemention was inspired by x4u who is a user contributing
 * to stackoverflow.com.
 * (https://stackoverflow.com/users/237321/x4u).
 *
 * @author dougestep
 */
public class HalfFloat
{
	private readonly short _halfPrecision;
	private float? _fullPrecision;

	/**
	 * Creates an instance of the class from the supplied the supplied
	 * byte array.  The byte array must be exactly two bytes in length.
	 *
	 * @param bytes the two-byte byte array.
	 */
	public HalfFloat(byte[] bytes)
	{
		if (bytes.Length != 2)
			throw new ArgumentException("The supplied byte array must be exactly two bytes in length");

		_halfPrecision = BitConverter.ToInt16(bytes);
	}

	/**
	 * Creates an instance of this class from the supplied short number.
	 *
	 * @param number the number defined as a short.
	 */
	public HalfFloat(short number)
	{
		_halfPrecision = number;
		_fullPrecision = ToFullPrecision();
	}

	/**
	 * Creates an instance of this class from the supplied
	 * full-precision floating point number.
	 *
	 * @param number the float number.
	 */
	public HalfFloat(float number)
	{
		if (number > short.MaxValue)
			throw new ArgumentException("The supplied float is too large for a two byte representation");
		if (number < short.MinValue)
			throw new ArgumentException("The supplied float is too small for a two byte representation");

		var val = FromFullPrecision(number);
		_halfPrecision = (short)val;
		_fullPrecision = number;
	}

	/**
	 * Returns the half-precision float as a number defined as a short.
	 *
	 * @return the short.
	 */
	public short GetHalfPrecisionAsShort()
	{
		return _halfPrecision;
	}

	/**
	 * Returns a full-precision floating pointing number from the
	 * half-precision value assigned on this instance.
	 *
	 * @return the full-precision floating pointing number.
	 */
	public float GetFullFloat()
	{
		_fullPrecision ??= ToFullPrecision();
		return _fullPrecision.Value;
	}

	/**
	 * Returns the full-precision float number from the half-precision
	 * value assigned on this instance.
	 *
	 * @return the full-precision floating pointing number.
	 */
	private float ToFullPrecision()
	{
		var mantisa = _halfPrecision & 0x03ff;
		var exponent = _halfPrecision & 0x7c00;

		if (exponent == 0x7c00)
			exponent = 0x3fc00;
		else if (exponent != 0)
		{
			exponent += 0x1c000;
			if (mantisa == 0 && exponent > 0x1c400)
				return BitConverter.Int32BitsToSingle((_halfPrecision & 0x8000) << 16 | exponent << 13);
		}
		else if (mantisa != 0)
		{
			exponent = 0x1c400;
			do
			{
				mantisa <<= 1;
				exponent -= 0x400;
			}
			while ((mantisa & 0x400) == 0);
			mantisa &= 0x3ff;
		}

		return BitConverter.Int32BitsToSingle((_halfPrecision & 0x8000) << 16 | (exponent | mantisa) << 13);
	}

	/**
	 * Returns the integer representation of the supplied
	 * full-precision floating pointing number.
	 *
	 * @param number the full-precision floating pointing number.
	 *
	 * @return the integer representation.
	 */
	private int FromFullPrecision(float number)
	{
		var fbits = BitConverter.SingleToInt32Bits(number);
		var sign = fbits >> 16 & 0x8000;

		var val = (fbits & 0x7fffffff) + 0x1000;

		if (val >= 0x47800000)
		{
			if ((fbits & 0x7fffffff) >= 0x47800000)
			{
				if (val < 0x7f800000)
					return sign | 0x7c00;
				return sign | 0x7c00 | (fbits & 0x007fffff) >> 13;
			}
			return sign | 0x7bff;
		}
		if (val >= 0x38800000)
			return sign | val - 0x38000000 >> 13;
		if (val < 0x33000000)
			return sign;
		val = (fbits & 0x7fffffff) >> 23;
		return sign | ((fbits & 0x7fffff | 0x800000) + (0x800000 >> val - 102) >> 126 - val);
	}
}
}