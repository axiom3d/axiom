
namespace TestNamespace {

	/// <summary> A test class.</summary>
	public class StoredNumber {

		/// <summary>Initializes the stored number class with a starting value.</summary>
		public StoredNumber (int value) {
			number = value;
		}

		private int number;

		/// <summary>Increments the stored number by one.</summary>
		public void Increment () {
			number++;
		}

		/// <summary>Increments the stored number by a specified step.</summary>
		public void Increment (int step) {
			number = number + step;
		}

		/// <summary>Gets the stored number.</summary>
		public int Value {
			get {
				return(number);
			}
		}	

	}

}
