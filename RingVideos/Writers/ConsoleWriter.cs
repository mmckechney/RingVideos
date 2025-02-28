using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;

namespace RingVideos.Writers
{
   public class ConsoleWriter
   {
      private ILogger<ConsoleWriter> log;
      private object lockObj = new object();
      private ThreadSafeList<LineWriter> lineWriters;
 

      public ConsoleWriter(ILogger<ConsoleWriter> log)
      {
         this.log = log;
         lineWriters = new ThreadSafeList<LineWriter>();
      }
      private void WriteMessage(string message, MessageType msgType = MessageType.Info)
      {
         try
         {
            Monitor.Enter(lockObj);
            var maxLine = GetMaxLineWriterLine();
            switch (msgType)
            {
               case MessageType.Highlight:
                  Console.ForegroundColor = ConsoleColor.Cyan;
                  break;
               case MessageType.Warning:
                  Console.ForegroundColor = ConsoleColor.Yellow;
                  break;
               case MessageType.Error:
                  Console.ForegroundColor = ConsoleColor.Red;
                  break;
               case MessageType.Info:
               default:
                  Console.ResetColor();
                  break;
            }
            if (maxLine > 0)
            {
               Console.SetCursorPosition(0, maxLine);
            }
            Console.WriteLine(message);
            Console.ResetColor();
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }
      public int GetMaxLineWriterLine()
      {
         if (lineWriters.Count > 0)
         {
            return lineWriters.Max(l => l.LinePosition);
         }
         else
         {
            return -1;
         }

      }
      public void ClearLineWriters()
      {
         lineWriters.Clear();
      }
      public void Warning(string message)
      {
         WriteMessage(message, MessageType.Warning);
         log.LogWarning(message);
      }
      public void Highlight(string message)
      {
         WriteMessage(message, MessageType.Highlight);
         log.LogInformation(message);
      }
      public void Info(string message)
      {
         WriteMessage(message);
         log.LogInformation(message);
      }
      public void Error(string message)
      {
         WriteMessage(message, MessageType.Error);
         log.LogError(message);
      }
      public LineWriter GetLineWriter()
      {
      
         LineWriter lw;
         try
         {
            Monitor.Enter(lockObj);
            if (Console.BufferHeight -1 == Console.CursorTop)
            {
               if (Console.BufferHeight - 1 == Console.CursorTop)
               {
                  lineWriters.ForEach(l => l.LinePosition--);
               }
            }

            Console.WriteLine();
            (_, int linePosition) = Console.GetCursorPosition();
            lw = new LineWriter(linePosition);
            lineWriters.Add(lw);
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
         return lw;
      }
      public void Write(LineWriter lw, string message)
      {
         try
         {
            if (lw.LinePosition < 0) lw.LinePosition = 0;
            Monitor.Enter(lockObj);
            Console.SetCursorPosition(0, lw.LinePosition);
            Console.Write(message);
            lw.InitialMessage = message;
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }
      internal void Update(LineWriter lw, string message, MessageType msgType)
      {
         try
         {
            Monitor.Enter(lockObj);
            //Console.SetCursorPosition(0, lw.LinePosition);
            //Console.Write(new string(' ', Console.WindowWidth));
            if(lw.LinePosition < 0) lw.LinePosition = 0;
            Console.SetCursorPosition(0, lw.LinePosition);
            Console.Write($"{lw.InitialMessage}  ");
            switch (msgType)
            {
               case MessageType.Highlight:
                  Console.ForegroundColor = ConsoleColor.Cyan;
                  break;
               case MessageType.Warning:
                  Console.ForegroundColor = ConsoleColor.Yellow;
                  break;
               case MessageType.Error:
                  Console.ForegroundColor = ConsoleColor.Red;
                  break;
               case MessageType.Final:
                  Console.ForegroundColor = ConsoleColor.Green;
                  break;
               case MessageType.Initial:
                  Console.ForegroundColor = ConsoleColor.Blue;
                  break;
               case MessageType.Info:
               default:
                  Console.ResetColor();
                  break;
            }

            Console.Write(message);
            Console.ResetColor();
            log.LogInformation($"{lw.InitialMessage}  {message}");
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }
      public void Update(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Initial);
      }
      public void UpdateFinal(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Final);
         //this.lineWriters.Remove(lw);
      }
      public void UpdateError(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Error);
      }
      public void UpdateWarning(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Warning);
      }

   }
}
