using System.Linq;
namespace umbraco.cms.presentation.create.controls
{
    using System;
    using System.Globalization;
    using System.Web;
    using System.Web.UI.WebControls;

    using umbraco.BasePages;
    using umbraco.cms.businesslogic.web;

    using Umbraco.Core;
    using Umbraco.Web;
    using Umbraco.Web.UI;

    /// <summary>
	///		Summary description for nodeType.
	/// </summary>
	public partial class nodeType : System.Web.UI.UserControl
	{
        /// <summary>
        /// The page_load.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            this.sbmt.Text = ui.Text("create");
            this.pp_name.Text = ui.Text("name");
            this.pp_MasterDocumentType.Text = ui.Text("masterDocumentType");
            this.createTemplate.Text = ui.Text("createMatchingTemplate");

            if (!IsPostBack)
            {
                string nodeId = Request.GetItemAsString("nodeId");
                if (String.IsNullOrEmpty(nodeId) || nodeId == "init")
                {
                    masterType.Items.Add(new ListItem(ui.Text("none") + "...", "0"));
                    foreach (DocumentType dt in DocumentType.GetAllAsList())
                    {
                        //                    if (dt.MasterContentType == 0)
                        masterType.Items.Add(new ListItem(dt.Text, dt.Id.ToString(CultureInfo.InvariantCulture)));
                    }

                    if (masterType.Items.Count == 1)
                    {
                        pp_mastertypes.Visible = false;
                    }
                }
                else
                {
                    // there's already a master doctype defined
                    masterType.Visible = false;
                    masterTypePreDefined.Visible = true;
                    masterTypePreDefined.Text = "<h3>" + new DocumentType(int.Parse(nodeId)).Text + "</h3>";
                }
            }
		}

        protected void validationDoctypeName(object sender, ServerValidateEventArgs e) {
            if (DocumentType.GetByAlias(rename.Text) != null)
                e.IsValid = false;
        }

        protected void validationDoctypeAlias(object sender, ServerValidateEventArgs e)
        {
            if (string.IsNullOrEmpty(rename.Text.ToSafeAlias()))
                e.IsValid = false;
        }

		protected void sbmt_Click(object sender, EventArgs e)
		{
			if (Page.IsValid) 
			{
				var createTemplateVal = 0;
			    if (createTemplate.Checked)
					createTemplateVal = 1;

                // check master type
                string masterTypeVal = String.IsNullOrEmpty(Request.GetItemAsString("nodeId")) || Request.GetItemAsString("nodeId") == "init" ? masterType.SelectedValue : Request.GetItemAsString("nodeId");

                // set master type to none if no master type was selected, or the drop down was hidden because there were no doctypes available
			    masterTypeVal = string.IsNullOrEmpty(masterTypeVal) ? "0" : masterTypeVal;

                var returnUrl = LegacyDialogHandler.Create(
                    new HttpContextWrapper(Context),
                    BasePage.Current.getUser(),
                    Request.GetItemAsString("nodeType"),
                    createTemplateVal,
					rename.Text,
                    int.Parse(masterTypeVal));

				BasePage.Current.ClientTools
					.ChangeContentFrameUrl(returnUrl)
					.ChildNodeCreated()
					.CloseModalWindow();

			}
		
		}
	}
}
