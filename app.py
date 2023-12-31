from flask import Flask, request, jsonify
from flask_cors import CORS 
import openai
from openai_logic.chatgpt import get_answer

app = Flask(__name__)
CORS(app,  resources={r"/api/*": {"origins": "http://localhost:3000"}})

PATH = "question_data/"
UNANSWERED_QUESTIONS_FILENAME = "unanswered_questions.txt"

# Initialize OpenAI with your API key
openai.api_key = "94acf53489374ddaaaeaf59e45edb351"

@app.route('/api/gpt3', methods=['POST'])
def run_gpt3():
    data = request.json
    prompt = data.get('prompt', '')
    #response = openai.Completion.create(engine="text-davinci-002", prompt=prompt, max_tokens=150)
    return jsonify(get_answer(prompt))

def add_question_to_report(text):
    try:
        with open(PATH + UNANSWERED_QUESTIONS_FILENAME, 'a') as file:
            file.write(text + '\n')
    except FileNotFoundError:
        with open(PATH + UNANSWERED_QUESTIONS_FILENAME, 'w') as file:
            file.write(text + '\n')
    except Exception as e:
        print(f"Error occurred: {e}")



if __name__ == '__main__':
    app.run(debug=True)