using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientHandling{

    public partial class MessageBuffer  {
		 #region Public Properties

		 public string OutgoingBuffer {
			 get {
				 string temp = "";
				 if (_outgoingBuffer.Count > 0) {
					 temp = _outgoingBuffer.Dequeue();
				 }

				 return temp;
			 }
			 set {
				 _outgoingBuffer.Enqueue(Format(value)); 
			 }
		 }

		 public byte[] OutgoingBytes {
			 get {
				 return System.Text.Encoding.ASCII.GetBytes(OutgoingBuffer);
			 }
			 set {
				 OutgoingBuffer = System.Text.Encoding.ASCII.GetString(value);
			 }
		 }
		 #endregion Public Properties

		 #region Private Members
		 private Queue<string> _outgoingBuffer;
		 #endregion Private Members

		 #region Constructors
		 public MessageBuffer(string id) {
			 _outgoingBuffer = new Queue<string>();
			 _incomingBuffer = new Queue<string>();
			 _telnetBuffer = new StringBuilder();
			 LogId = id;
		 }
		 #endregion Constructors

		 #region Private Methods
		 //this method will take in a string and parse it so that there is a new line every 80 characters
		 //without splitting words, it will go back to the previous space, comma, period, etc.
		 private static string Format(string input) {		
             if (input == null || input.Length == 0) return "";

			 StringBuilder sb = new StringBuilder();
			 int index = 0;
			 int max = 79;
			 
			 while (index < input.Length){
				 //let's get the length of one line out of whats left of the original string
				 int maxChars = max > input.Length ? input.Length : max;
				 //let's get the line from the rest of the string
                 string temp2 = input.Substring(0, maxChars);
                 while (true) {
                     if (temp2.Contains("\x1b[")) {
                         maxChars += 2;
                         temp2 = temp2.Substring(temp2.IndexOf("\x1b[") + 2);
                     }
                     else {
                         maxChars = maxChars > input.Length ? input.Length : maxChars;
                         break;
                     }
                 }
                 string temp = input.Substring(0, maxChars);
				 //this is the last character in the line
				 int tempIndex = temp.Length - 1;
				 //if last character is a letter and the next one is not whitespace, or the line contains a new line
				 //we need to go back to the first newline or the last whitespace to not split a word in half and look like amateurs
				 char test = temp[tempIndex];
				 if (input.Length > max && ((Char.IsLetter(temp[tempIndex]) || Char.IsPunctuation(temp[tempIndex])) && (!Char.IsWhiteSpace(input[tempIndex]))) || temp.Contains("\r\n")) {
					 index = temp.IndexOf("\r\n"); //find the first newline break
					 if (index == -1) index = temp.LastIndexOf(" ") + 1; //if no newline then get closest whitespace to end of string 
					 else index += 2; //we found \r\n and we are moving the end of it
					 if (index == 0) index = maxChars;
					 temp = input.Substring(0, index); //this is now the actual substring
				 }
				 index = temp.Length; //in case it didn't get set in the IF statement above
		       //make it a new line and get rid of the\r\n and any starting white spaces
				 sb.AppendLine(temp.Replace("\r", "").Replace("\n","").TrimEnd(' '));
							 
				 input = input.Substring(index);
				 index = 0;
			 } 
			 return sb.ToString();
		 }
		 #endregion
	 }
}
