using System;
using System.ComponentModel;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.IdentityModel.Claims;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;

namespace ClaimsWebPart.ClaimsWebPart
{
    // http://sharepointpals.com/post/How-to-Read-claims-of-a-User-in-SharePoint-2013
    [ToolboxItemAttribute(false)]
    public class ClaimsWebPart : WebPart
    {
        protected override void CreateChildControls()
        {
            var claimsUser = Page.User as IClaimsPrincipal;
            if (claimsUser != null)
            {
                DataRow dr;
                DataTable claimsTable = new DataTable();
                claimsTable.Columns.Add("Type", typeof(string));
                claimsTable.Columns.Add("Value", typeof(string));

                IClaimsIdentity ci = (IClaimsIdentity)claimsUser.Identity;
                foreach (Claim c in ci.Claims)
                {
                    dr = claimsTable.NewRow();
                    dr["Type"] = c.ClaimType.ToString();
                    dr["Value"] = c.Value.ToString();
                    claimsTable.Rows.Add(dr);
                }

                // Standard SPGridView to display our claims table
                SPGridView claimsGrid = new SPGridView();

                // This eventhandler is used to add the word-break style
                //claimsGrid.RowDataBound += new GridViewRowEventHandler(claimsGrid_RowDataBound);

                // AutoGenerate must be false for SPGridView
                claimsGrid.AutoGenerateColumns = false;
                claimsGrid.DataSource = claimsTable;

                SPBoundField boundField;

                boundField = new SPBoundField();
                boundField.HeaderText = "Type";
                boundField.HeaderStyle.HorizontalAlign = HorizontalAlign.Left;
                boundField.DataField = "Type";
                claimsGrid.Columns.Add(boundField);

                boundField = new SPBoundField();
                boundField.HeaderText = "Value";
                boundField.HeaderStyle.HorizontalAlign = HorizontalAlign.Left;
                boundField.DataField = "Value";
                claimsGrid.Columns.Add(boundField);

                for (int i = 0; i < claimsGrid.Columns.Count; i++)
                {
                    claimsGrid.Columns[i].ItemStyle.Wrap = true;
                    // Distribute the columns evenly
                    claimsGrid.Columns[i].ItemStyle.Width = Unit.Percentage(100 / claimsGrid.Columns.Count);
                }

                claimsGrid.DataBind();

                this.Controls.Add(claimsGrid);
            }
        }
    }
}
