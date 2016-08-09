using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorldBuilder
{
    public partial class Form1 : Form
    {
        private void createDefaultDatabase_Click(object sender, EventArgs e)
        {
            //create the database with default data 
            //we'll just restore a default dump file
        }

        private void deleteDatabase_Click(object sender, EventArgs e)
        {
            //wipe the entire database
        }

        private void restoreDatabase_Click(object sender, EventArgs e)
        {
            //restore database from dump file
        }

        private void createDumpFile_Click(object sender, EventArgs e)
        {
            //create a dump file
        }

        private void selectRestoreFile_Click(object sender, EventArgs e)
        {
            //open Open file dialog
        }

        private void selectSaveDumpFile_Click(object sender, EventArgs e)
        {
            //save dump file
        }
    }
}
