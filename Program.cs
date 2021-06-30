using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Mail
{
    class Program
    {
        static void Main(string[] args)
        {
            var input = ' ';
            while (input!='0'&&input!='1')
            {
                Console.WriteLine("Welches Protokoll möchten sie Nutzen?");
                Console.WriteLine("  0: IMAP (TLS)");
                Console.WriteLine("  1: SMTP (STARTTLS)");
                Console.WriteLine();
                Console.Write("Eingabe: ");
                input = Console.ReadKey().KeyChar;
                Console.WriteLine();
                Console.WriteLine(input);
            }
            
            if (input == '0')
            {
                Imap();
            }
            else
            {
                Smtp();
            }
        }

        private static void Smtp()
        {
            SslStream sslStream = EstablishSmtpConnection();
            Task.Run(() => ReceiveMessages(sslStream));
            
            //Console.WriteLine("Dbg: Connection Established.");
            SendMessage("HELO lernsax.de", sslStream);
            
            HandleConsoleInputs(sslStream);
        }

        private static void Imap()
        {
            var sslStream = EstablishImapConnection();
            Task.Run(() => ReceiveMessages(sslStream));

            HandleConsoleInputs(sslStream);
        }

        private static void HandleConsoleInputs(SslStream sslStream)
        {
            string message;
            while (sslStream != null)
            {
                message = Console.ReadLine();
                if (message.ToUpper().StartsWith("AUTH LOGIN"))
                {
                    SendMessage(message, sslStream);
                    Console.WriteLine("(334 Username:)");
                    message = Convert.ToBase64String(Encoding.UTF8.GetBytes(Console.ReadLine()));
                    SendMessage(message, sslStream);
                    Console.WriteLine(message);
                    Console.WriteLine("(334 Password:)");
                    message = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetPassword()));
                    Console.WriteLine(message);
                }

                if (message.ToUpper().Trim().EndsWith("AUTHENTICATE"))
                {
                    string b64 = char.ConvertFromUtf32(0);
                    Console.Write("E-Mail: ");
                    b64 += Console.ReadLine();
                    b64 += char.ConvertFromUtf32(0);
                    Console.Write("Password: ");
                    b64 += GetPassword();

                    message += " PLAIN " + Convert.ToBase64String(Encoding.UTF8.GetBytes(b64));
                    Console.WriteLine(message);
                }
                SendMessage(message, sslStream);
                Console.WriteLine();
            }
        }

        private static SslStream EstablishImapConnection()
        {
            TcpClient client = new TcpClient();
            SslStream sslStream;
            
            client.Connect("mail.lernsax.de",993);
            sslStream = new SslStream(client.GetStream());
            
            sslStream.AuthenticateAsClient("mail.lernsax.de", null, SslProtocols.Tls12, true);

            return sslStream;
        }
        
        private static SslStream EstablishSmtpConnection()
        {
            TcpClient client = new TcpClient();
            SslStream sslStream;
            
            //Console.WriteLine("Dbg: Opening TCP Connection.");
            client.Connect("mail.lernsax.de",587);
            //Console.WriteLine("Dbg: Connection Opened");
            
            //Console.WriteLine("Dbg: Creating Buffers");
            StreamReader reader = new StreamReader(client.GetStream());
            StreamWriter writer = new StreamWriter(client.GetStream()){AutoFlush = true};
            
            //Console.WriteLine("Dbg: Reading First Line");
            Console.WriteLine(reader.ReadLine());

            //Console.WriteLine("Dbg: Sending HELO.");
            writer.WriteLine("EHLO lernsax.de");

            string message = "";

            do
            {
                message = reader.ReadLine();
                Console.WriteLine(message);
            } while (!message.StartsWith("250 "));
            
            //Console.WriteLine("Dbg: STARTTLS");
            writer.WriteLine("STARTTLS");
            //Console.WriteLine("Dbg: STARTTLS Send");

            Console.WriteLine(reader.ReadLine());
            
            //Console.WriteLine("Dbg: Opening SSL Stream");
            sslStream = new SslStream(client.GetStream());
            sslStream.AuthenticateAsClient("mail.lernsax.de", null, SslProtocols.Tls12, true);

            return sslStream;
        }

        private static void SendMessage(string text, SslStream sslStream)
        {
            sslStream.Write(Encoding.UTF8.GetBytes(text + "\r\n"));
        }

        private static void ReceiveMessages(SslStream sslStream)
        {
            StreamReader reader = new StreamReader(sslStream);
            while (!reader.EndOfStream)
            {
                Console.WriteLine(reader.ReadLine());
            }
            
            Console.WriteLine("Stream Closed...Ending Program");
            Environment.Exit(0);
        }

        private static void ReadMessage(StreamReader reader)
        {
            Console.WriteLine(reader.ReadLine());
        }

        private static string GetPassword()
        {
            var pass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            
            Console.WriteLine();

            return pass;
        }
    }
}
