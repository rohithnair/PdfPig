﻿namespace UglyToad.PdfPig.UglyViewer
{
    using System;
    using System.Windows.Forms;

    public partial class frmNotepad : Form
    {
        public void Start(string content)
        {
            txtNotepad.Text = content;
            Show();
        }


        public frmNotepad()
        {
            InitializeComponent();
        }
    }
}
