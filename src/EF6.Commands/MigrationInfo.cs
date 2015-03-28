using System;

namespace EF6.Commands
{
    public class MigrationInfo
    {
        public String Id { get; set; }
        
        public Boolean InDatabase { get; set; }
        
        public Boolean InProject { get; set; }
    }
}