using MaIN.Models;
using MaIN.Services.Mappers;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class RagService(IOllamaService ollamaService) : IRagService
{
  private string testData =
    """
    [
        {
          "id": 1,
          "image": "pc1.jpg",
          "type": "PC",
          "name": "Gaming PC Ultra",
          "brand": "PowerTech",
          "processor": "Intel Core i9-11900K",
          "ram": "32GB DDR4",
          "storage": "1TB SSD + 2TB HDD",
          "gpu": "NVIDIA GeForce RTX 3090",
          "price": 2999.99,
          "availability": "In Stock",
          "description": "The Gaming PC Ultra from PowerTech features an Intel Core i9 processor and NVIDIA RTX 3090, making it perfect for high-end gaming and demanding applications. With 32GB of RAM and ample storage, this PC can handle any task with ease."
        },
        {
          "id": 2,
          "image": "pc2.png",
          "type": "PC",
          "name": "Workstation Pro",
          "brand": "TechMaster",
          "processor": "AMD Ryzen 9 5950X",
          "ram": "64GB DDR4",
          "storage": "2TB SSD",
          "gpu": "NVIDIA Quadro RTX 5000",
          "price": 3499.99,
          "availability": "In Stock",
          "description": "TechMaster's Workstation Pro is designed for professionals who require top-tier performance. With an AMD Ryzen 9 CPU, 64GB RAM, and NVIDIA Quadro RTX GPU, this workstation excels in 3D rendering, video editing, and other intensive tasks."
        },
        {
          "id": 3,
          "image": "pc3.jpg",
          "type": "PC",
          "name": "Budget Gamer",
          "brand": "EconoPC",
          "processor": "Intel Core i5-10400F",
          "ram": "16GB DDR4",
          "storage": "512GB SSD",
          "gpu": "NVIDIA GeForce GTX 1660 Super",
          "price": 799.99,
          "availability": "Out of Stock",
          "description": "The Budget Gamer by EconoPC provides excellent value for entry-level gaming. Featuring an Intel Core i5 processor and NVIDIA GTX 1660 Super GPU, this PC delivers smooth performance for popular games at an affordable price."
        },
        {
          "id": 4,
          "image": "pc4.jpg",
          "type": "PC",
          "name": "All-Purpose PC",
          "brand": "ValueComp",
          "processor": "AMD Ryzen 5 3600",
          "ram": "16GB DDR4",
          "storage": "1TB HDD",
          "gpu": "AMD Radeon RX 5700",
          "price": 999.99,
          "availability": "In Stock",
          "description": "ValueComp's All-Purpose PC is a versatile system suitable for gaming, work, and everyday use. It features an AMD Ryzen 5 processor, 16GB RAM, and a Radeon RX 5700 GPU, ensuring balanced performance for a variety of tasks."
        },
        {
          "id": 5,
          "image": "pc5.jpg",
          "type": "PC",
          "name": "Compact Office PC",
          "brand": "OfficeMate",
          "processor": "Intel Core i3-10100",
          "ram": "8GB DDR4",
          "storage": "256GB SSD",
          "gpu": "Integrated Graphics",
          "price": 499.99,
          "availability": "In Stock",
          "description": "The Compact Office PC by OfficeMate is ideal for small spaces and routine office tasks. Equipped with an Intel Core i3 processor and 8GB of RAM, it offers reliable performance for word processing, spreadsheets, and internet browsing."
        },
        {
          "id": 6,
          "image": "pc6.jpg",
          "type": "PC",
          "name": "Performance Beast",
          "brand": "ExtremeTech",
          "processor": "Intel Core i7-12700K",
          "ram": "32GB DDR5",
          "storage": "2TB SSD",
          "gpu": "NVIDIA GeForce RTX 3080",
          "price": 2599.99,
          "availability": "In Stock",
          "description": "ExtremeTech's Performance Beast is designed for the most demanding users. With an Intel Core i7 processor, 32GB DDR5 RAM, and NVIDIA RTX 3080 GPU, this PC is ideal for gaming, 3D rendering, and other high-performance applications."
        },
        {
          "id": 7,
          "image": "laptop1.jpg",
          "type": "Laptop",
          "name": "UltraBook Pro",
          "brand": "PowerTech",
          "processor": "Intel Core i7-1165G7",
          "ram": "16GB LPDDR4x",
          "storage": "512GB SSD",
          "gpu": "Intel Iris Xe Graphics",
          "price": 1299.99,
          "availability": "In Stock",
          "description": "The UltraBook Pro by PowerTech offers a sleek design with powerful performance, featuring an Intel Core i7 processor and Intel Iris Xe Graphics. With 16GB of RAM and a 512GB SSD, this laptop is perfect for professionals on the go."
        },
        {
          "id": 8,
          "image": "laptop2.jpg",
          "type": "Laptop",
          "name": "Gaming Laptop X",
          "brand": "TechMaster",
          "processor": "AMD Ryzen 7 5800H",
          "ram": "32GB DDR4",
          "storage": "1TB SSD",
          "gpu": "NVIDIA GeForce RTX 3070",
          "price": 1999.99,
          "availability": "In Stock",
          "description": "TechMaster's Gaming Laptop X is designed for gamers who need high performance on the go. Featuring an AMD Ryzen 7 CPU and NVIDIA RTX 3070 GPU, this laptop delivers smooth gaming experiences and fast load times."
        },
        {
          "id": 9,
          "image": "laptop3.jpg",
          "type": "Laptop",
          "name": "Budget Laptop",
          "brand": "EconoPC",
          "processor": "Intel Core i3-1115G4",
          "ram": "8GB DDR4",
          "storage": "256GB SSD",
          "gpu": "Integrated Graphics",
          "price": 499.99,
          "availability": "Out of Stock",
          "description": "The Budget Laptop by EconoPC offers essential features at an affordable price. With an Intel Core i3 processor and 8GB of RAM, it's suitable for students and casual users who need reliable performance for everyday tasks."
        },
        {
          "id": 10,
          "image": "laptop4.jpg",
          "type": "Laptop",
          "name": "All-Purpose Laptop",
          "brand": "ValueComp",
          "processor": "AMD Ryzen 5 4500U",
          "ram": "16GB DDR4",
          "storage": "512GB SSD",
          "gpu": "AMD Radeon Graphics",
          "price": 899.99,
          "availability": "In Stock",
          "description": "ValueComp's All-Purpose Laptop is versatile and powerful, equipped with an AMD Ryzen 5 processor and Radeon Graphics. It's ideal for a variety of tasks, including work, entertainment, and light gaming."
        },
        {
          "id": 11,
          "image": "laptop5.jpg",
          "type": "Laptop",
          "name": "Compact Office Laptop",
          "brand": "OfficeMate",
          "processor": "Intel Core i5-1135G7",
          "ram": "8GB LPDDR4x",
          "storage": "512GB SSD",
          "gpu": "Intel Iris Xe Graphics",
          "price": 799.99,
          "availability": "In Stock",
          "description": "The Compact Office Laptop by OfficeMate is perfect for professionals who need a portable and efficient device. With an Intel Core i5 processor and 8GB of RAM, it offers solid performance for office applications and multitasking."
        },
        {
          "id": 12,
          "image": "laptop6.jpg",
          "type": "Laptop",
          "name": "Creative Laptop",
          "brand": "CreativeTech",
          "processor": "Apple M1",
          "ram": "16GB Unified Memory",
          "storage": "1TB SSD",
          "gpu": "Apple M1 GPU",
          "price": 1499.99,
          "availability": "In Stock",
          "description": "CreativeTech's Creative Laptop is perfect for designers and content creators. Featuring the Apple M1 chip, 16GB of unified memory, and a 1TB SSD, this laptop offers exceptional performance and efficiency for creative tasks."
        }
    ]
    """;
    public async Task<Chat> Completions(Chat chat, bool translatePrompt = false)
    {
        if (!chat.Messages.Any())
        {
          
          var message = new Message()
          {
            Content =
              "You are shop assistant that works in a PC store called 'GeekITStuff', do your best to serve customers and provide good " +
              "help with their questions. If you will be asked about products or recommendation, this is data that you can use:" +
              $"{testData}",
            Role = "system"
          };
         
            chat.Messages.Add(message);
        }
        
        var result = await ollamaService.Send(chat);
        chat.Messages.Add(result!.Message.ToDomain());
        return chat;
    }
}