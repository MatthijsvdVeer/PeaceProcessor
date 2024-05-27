import getpass
import os
import json

from langchain_core.output_parsers import StrOutputParser
from langchain_core.prompts import ChatPromptTemplate
from langchain_openai import ChatOpenAI

# pip install langchain-prompty
from langchain_prompty import create_chat_prompt
from pathlib import Path

# load prompty as langchain ChatPromptTemplate
# Important Note: Langchain only support mustache templating. Add 
#  template: mustache
# to your prompty and use mustache syntax.
folder = Path(__file__).parent.absolute().as_posix()
path_to_prompty = folder + "/basic.prompty"
prompt = create_chat_prompt(path_to_prompty)

os.environ["OPENAI_API_KEY"] = getpass.getpass()
model = ChatOpenAI(model="gpt-4")


output_parser = StrOutputParser()

chain = prompt | model | output_parser

json_input = '''{
  "topic": "Gratitude"
}'''
args = json.loads(json_input)
result = chain.invoke(args)
print(result)
