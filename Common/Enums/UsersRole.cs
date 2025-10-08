using ResourceLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Common.Enums
{
    public enum UsersRole
    {
        [Display(Name = "Admin", ResourceType = typeof(ResourceLibrary.Panel.Admin.Users.Users))]
        admin = 1,
        [Display(Name = "User", ResourceType = typeof(ResourceLibrary.Panel.Admin.Users.Users))]
        user = 2,
        [Display(Name = "مدیر")]
        manager =3,            
        [Display(Name = "مربی")]
        coach =4,              
        [Display(Name = "ورزشکار")]
        athlete =5
    }

}
