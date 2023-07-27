import React, { useState } from 'react';
import axios from 'axios';
import styled from 'styled-components';
import logo from './logo.svg';

const App = () => {
  const [inputText, setInputText] = useState('');
  const [messages, setMessages] = useState([]);

  const handleInputChange = (e) => {
    setInputText(e.target.value);
  };

  const handleSubmit = async () => {
    console.log("submitting");
    try {
      const response = await axios.post('http://localhost:5000/api/gpt3', { prompt: inputText });
      console.log("test");
      console.log(response);
      const botResponse = response.data;
      setMessages([...messages, { user: inputText, bot: botResponse }]);
      setInputText('');
    } catch (error) {
      console.error('Error:', error);
      const errorMessage = 'An error occurred while processing your request.';
      setMessages([...messages, { user: inputText, bot: errorMessage }]);
      setInputText('');
    }
  };

  return (
    <Container>
      <ContentContainer>
        <Head>
          <img src={logo} alt="Zocdoc logo" />
          <h1>ZocBot</h1>
        </Head>
        <ChatContainer>
          {messages.map((message, index) => (
            <div>
            <ChatMessage key={index}>
              <strong>You:</strong> {message.user}
            </ChatMessage>
            <ChatMessage key={index + 1}>
              <strong>ZocBot:</strong> {message.bot}
            </ChatMessage>
            </div>
          ))}
        </ChatContainer>
        <StyledTextarea
          rows={4}
          cols={50}
          value={inputText}
          onChange={handleInputChange}
          placeholder="Enter your prompt here..."
        />
        <StyledButton onClick={handleSubmit}>Submit</StyledButton>
      </ContentContainer>
    </Container>
  );
};

const Container = styled.div`
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background-color: #FEED5A;
`;

const ContentContainer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
`;

const Head = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 20px;

  img {
    width: 100px;
    height: 100px;
  }

  h1 {
    margin-top: 10px;
    font-size: 24px;
    color: #00234B;
  }
`;

const ChatContainer = styled.div`
  background-color: #ffffff;
  border: 1px solid #ccc;
  border-radius: 5px;
  padding: 10px;
  width: 400px;
  height: 300px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  align-items: flex-start;
  margin-bottom: 20px;
`;

const ChatMessage = styled.div`
  margin-bottom: 5px;
`;

const StyledTextarea = styled.textarea`
  width: 100%;
  max-width: 400px;
  resize: vertical;
  padding: 5px;
  margin-bottom: 10px;
`;

const StyledButton = styled.button`
  background-color: #00234B;
  color: #ffffff;
  padding: 15px 20px;
  border: none;
  border-radius: 10px;
  cursor: pointer;
  font-size: 15px;

  &:hover {
    background-color: #00152D;
  }
`;

export default App;
