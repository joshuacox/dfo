
using System;
using Gtk;
using Glade;

	public class PhotoEditorUI
	{
    [Glade.Widget]
    Window window2;
    
    [Glade.Widget]
    Notebook notebook1;
    
    [Glade.Widget]
    Table table1;
    
    [Glade.Widget]
    Label label2;
    
    [Glade.Widget]
    Label label3;
    
    [Glade.Widget]
    Label label4;
    
    [Glade.Widget]
    Label label5;
    
    [Glade.Widget]
    Entry entry1;
    
    [Glade.Widget]
    Entry entry2;
    
    [Glade.Widget]
    ComboBox combobox1;
    
    [Glade.Widget]
    ComboBox combobox2;
    
    [Glade.Widget]
    TreeView treeview3;
    
    [Glade.Widget]
    TextView textview3;
    
    [Glade.Widget]
    Button button3;
    
    [Glade.Widget]
    Button button4;
    
    [Glade.Widget]
    CheckButton checkbutton1;
    
    [Glade.Widget]
    Button button5;
    
    [Glade.Widget]
    Button button6;
    
    
		public PhotoEditorUI()
		{
		  Glade.XML gxml = new Glade.XML (null, "organizer.glade", "window2", null);
		  gxml.Autoconnect (this);
		  
		  notebook1.SetTabLabelText(notebook1.CurrentPageWidget, "Information");
		  notebook1.NextPage();
		  notebook1.SetTabLabelText(notebook1.CurrentPageWidget, "Tags");
		  notebook1.PrevPage();
		  
		  table1.SetColSpacing(0, 50);
		  
		  // Set Labels
		  label2.Text = "Title:";
		  label3.Text = "Description:";
		  label4.Text = "Visibility:";
		  label5.Text = "License:";
		  label2.Justify = Justification.Left;
		  label3.Justify = Justification.Left;
		  label4.Justify = Justification.Left;
		  label5.Justify = Justification.Left;
		  
		  // Set Combo boxes - Privacy Level
		  combobox1.AppendText(" ");
		  combobox1.AppendText("Public");
		  combobox1.AppendText("Only Friends & Family");
		  combobox1.AppendText("Only Friends");
		  combobox1.AppendText("Only Family");
		  combobox1.AppendText("Private");
		  
		  // Set Combo boxes - License
		  combobox2.AppendText("");
		  combobox2.AppendText("Attribution-NonCommercial-ShareAlike License");
		  combobox2.AppendText("Attribution-NonCommercial License");
		  combobox2.AppendText("Attribution-NonCommercial-NoDerivs License");
		  combobox2.AppendText("Attribution License");
		  combobox2.AppendText("Attribution-ShareAlike License");
		  combobox2.AppendText("Attribution-NoDerivs License");
		  combobox2.AppendText("All Rights Reserved");
		  
		  Console.WriteLine("Should show up the editor now");
		  window2.ShowAll();
		}
	}