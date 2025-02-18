namespace RingVideos.Writers
{
   public class LineWriter
   {
      public int LinePosition { get; set; }
      public string InitialMessage { get; set; } = "";
      public LineWriter(int linePosition)
      {
         LinePosition = linePosition;
      }

   }
}
