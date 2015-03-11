using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SlashTodo.Web.Api
{
    public class SlashCommandResponseTexts : ISlashCommandResponseTexts
    {
        private const string NewLine = "\n";

        public string UnknownCommand(SlashCommand command)
        {
            return string.Format("Unknown command. Use `{0} help` for instructions.", command.Command);
        }

        public string UsageInstructions(SlashCommand command)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("*Usage instructions for {0}*{1}", command.Command, NewLine);
            builder.AppendFormat("`{0}` Show task list (to yourself only).{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} show` Show task list to everyone in the channel.{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} add [description]` Add a task.{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} remove [id] [--force]` Remove the task with id [id].{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} tick [id] [--force]` Mark the task with id [id] as completed.{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} untick [id]` Mark the task with id [id] as not completed.{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} claim [id] [--force]` Take ownership of the task with id [id].{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} free [id] [--force]` Release ownership of the task with id [id].{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} trim` Remove all completed tasks.{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} clear [--force]` Remove all tasks.{1}", command.Command, NewLine);
            builder.AppendFormat("`{0} help` Show these instructions.{1}", command.Command, NewLine);
            builder.Append(NewLine);
            builder.AppendFormat("Claiming a task prevents others from ticking or removing it. ");
            builder.AppendFormat("Use the `--force` switch to override this behavior.");
            return builder.ToString();
        }
    }
}