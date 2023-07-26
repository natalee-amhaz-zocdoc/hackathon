namespace FaqChatbot.Model;

public class PaginatedResponse<T>
{
   public int Limit;
   public string PageToken;
   public T Data;
}
