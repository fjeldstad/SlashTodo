# Registration flow

1. User browses to https://slashtodo.com/signup
2. User logs in with Slack (via OAuth2 redirect).
3. If the user is not a team admin, deny access.
4. If the team that the user belongs to is already registered,
   redirect to the account management page.
5. If the team is not registered, create a new account
   and redirect to the account management page.

   Account management page: 
     Team name
	 Slash command url
	 Slash command token (edit)
	 Incoming webhook url (edit)

# Supported commands

    /todo add {text}
	/todo trim
	/todo
	/todo show
	/todo tick|check {reference} [--force]
	/todo untick|uncheck {reference} [--force]
	/todo remove {reference} [--force]
	/todo clear [--force]
	/todo claim {reference} [--force]
	/todo free {reference} [--force]

