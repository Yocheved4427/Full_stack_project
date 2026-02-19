

namespace DTOs
{
    public record ProductDTO(
     
     int ProductId,

     string ProductName,

     string Description,

     int CategoryId,

     decimal Price,

     string MainImageUrl,
   
    List<string> ImageUrls,

    List<ProductMonthConfigDTO> MonthConfigs);
    

}
