﻿using Common;
using Common.Consts;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
	public class JwtService : IJwtService, IScopedDependency
	{
		private readonly ProjectSettings _siteSetting;
		private readonly SignInManager<ApplicationUser> signInManager;
		private readonly IRepository<AspNetUserJwtRefreshToken> _JwtTableRepository;
	 
		public JwtService(IOptionsSnapshot<ProjectSettings> settings, SignInManager<ApplicationUser> signInManager,
			IRepository<AspNetUserJwtRefreshToken> JwtTableRepository)
		{
			_siteSetting = settings.Value;
			this.signInManager = signInManager;
			_JwtTableRepository = JwtTableRepository;
	 
		}

		public async Task<AccessToken> GenerateAsync(ApplicationUser user)
		{
			var secretKey = Encoding.UTF8.GetBytes(_siteSetting.JwtSettings.SecretKey); // longer that 16 character
			var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature);

			var claims = await _getClaimsAsync(user);

			var descriptor = new SecurityTokenDescriptor
			{
				Issuer = _siteSetting.JwtSettings.Issuer,
				Audience = _siteSetting.JwtSettings.Audience,
				IssuedAt = DateTime.Now,
				NotBefore = DateTime.Now.AddMinutes(_siteSetting.JwtSettings.NotBeforeMinutes),
				Expires = DateTime.Now.AddMinutes(_siteSetting.JwtSettings.ExpirationMinutes),
				SigningCredentials = signingCredentials,
				Subject = new ClaimsIdentity(claims)
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			var securityToken = tokenHandler.CreateJwtSecurityToken(descriptor);
			return new AccessToken(securityToken);
		}

		private async Task<IEnumerable<Claim>> _getClaimsAsync(ApplicationUser user)
		{
			var result = await signInManager.ClaimsFactory.CreateAsync(user);
			//add custom claims
			var list = new List<Claim>(result.Claims);
		
			return list;
		}
	}
}
