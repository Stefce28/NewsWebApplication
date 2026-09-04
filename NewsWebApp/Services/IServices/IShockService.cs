using NewsWebApp.Models;
using NewsWebApp.ViewModels;

namespace NewsWebApp.Services.IServices;

public interface IShockService
{
    decimal GetCurrentShock();
    ShockStatusViewModel GetShockStatus();
    decimal AddShockFromArticle(Article article);
    bool HasReadArticle(Guid articleId);
    void MarkArticleAsRead(Guid articleId);
    void ResetShock();
}
