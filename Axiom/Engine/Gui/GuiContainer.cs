using System;

namespace Axiom.Gui
{
	/// <summary>
	/// 	Summary description for GuiContainer.
	/// </summary>
	public class GuiContainer : GuiElement
	{
		#region Member variables
		
		#endregion
		
		#region Constructors
		
		public GuiContainer(string name) : base(name) {
		}
		
		#endregion
		
		#region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        public void AddChild(GuiElement element) {
        }

        public override void Initialize() {
            // TODO: Figure out what to do with this.
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void UpdatePositionGeometry() {

        }

		
		#endregion
		
		#region Properties
		
        public override String Type {
            get {
                return null;
            }
        }

		#endregion

	}
}
