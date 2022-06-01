using System.Windows.Forms;

namespace AliumV1
{
    public class Warning
    {
        public static void Main()
        {
            string message = "You ran a viurs called `AliumV1`, which will harm your computer.\r\n" +
                "If you don't know what you've just ran, simply press `No` and nothing bad will happen to your computer.\r\n\r\n" + "I recommend you to test this virus on a virtual machine!";

            string title = "Hey! Do you even know what you've executed?";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result = MessageBox.Show(message, title, buttons, MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                string goodmsg = "Your computer is safe now!";
                string goodtitle = "Yay!";
                MessageBoxButtons goodbuttons = MessageBoxButtons.OK;
                DialogResult goodresult = MessageBox.Show(goodmsg, goodtitle, goodbuttons, MessageBoxIcon.Information);
            }

            if (result == DialogResult.Yes)
            {
                string message2 = "ARE YOU SURE YOU WANT TO RUN THIS VIRUS?!\r\n" + "THE CREATOR ISN'T RESPONSIBLE FOR ANY DAMAGE CAUSED TO YOU BY RUNNING THIS VIRUS!\r\n\r\n\r\n" + "DO YOU STILL WANT TO EXECUTE IT?";
                string title2 = "FINAL WARNING!";
                MessageBoxButtons buttons2 = MessageBoxButtons.YesNo;
                DialogResult result2 = MessageBox.Show(message2, title2, buttons2, MessageBoxIcon.Warning);

                if (result2 == DialogResult.No)
                {
                    string goodmsg2 = "Your computer is safe now!";
                    string goodtitle2 = "Yay!";
                    MessageBoxButtons goodbuttons2 = MessageBoxButtons.OK;
                    DialogResult goodresult2 = MessageBox.Show(goodmsg2, goodtitle2, goodbuttons2, MessageBoxIcon.Asterisk);
                }

                if (result2 == DialogResult.Yes)
                {
                    MainClass.Main();
                }
            }
        }
    }
}