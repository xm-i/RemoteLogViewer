using System;

namespace RemoteLogViewer.Composition.Utils.Objects;

public class ColorModel : IEquatable<ColorModel> {
	public byte R {
		get;
		set;
	}
	public byte G {
		get;
		set;
	}
	public byte B {
		get;
		set;
	}
	public byte A {
		get;
		set;
	}
	public static ColorModel FromArgb(byte a, byte r, byte g, byte b) {
		return new ColorModel {
			A = a,
			R = r,
			G = g,
			B = b
		};
	}

	// Value equality
	public bool Equals(ColorModel? other) {
		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return this.A == other.A && this.R == other.R && this.G == other.G && this.B == other.B;
	}

	public override bool Equals(object? obj) {
		return obj is ColorModel cm && this.Equals(cm);
	}

	public override int GetHashCode() {
		return HashCode.Combine(this.A, this.R, this.G, this.B);
	}

	public static bool operator ==(ColorModel? left, ColorModel? right) {
		return Equals(left, right);
	}

	public static bool operator !=(ColorModel? left, ColorModel? right) {
		return !Equals(left, right);
	}

	public override string ToString() {
		return $"#{this.A:X2}{this.R:X2}{this.G:X2}{this.B:X2}";
	}
}
