import React, { useState } from 'react';
import axios from 'axios';
import styled from 'styled-components';
import logo from './logo.svg';

const App = () => {
  const [inputText, setInputText] = useState('');
  const [responseText, setResponseText] = useState('');

  const handleInputChange = (e) => {
    setInputText(e.target.value);
  };

  const handleSubmit = async () => {
    console.log("wubmitting");
    try {
      const response = await axios.post('http://localhost:5000/api/gpt3', { prompt: inputText });
      console.log("test");
      console.log(response);
      setResponseText(response.data.choices[0].text);
    } catch (error) {
      console.error('Error:', error);
      setResponseText('An error occurred while processing your request.');
    }
  };

  return (
    <Container>
      <Head>
        <img src={logo} alt="Zocdoc logo"/>
        <h1>ZocBot</h1>
        <textarea
          rows={10}
          cols={50}
          value={inputText}
          onChange={handleInputChange}
          placeholder="Enter your prompt here..."
        />
        <br />
        <button onClick={handleSubmit}>Submit</button>
        <div>
          <h2>Response:</h2>
          <p>{responseText}</p>
        </div>
      </Head>
    </Container>
  );
};

const Container = styled.div`
  position: relative
  display: flex;
  justify content: center;
  align-items: center;
  height: 100vh;
  background-color:  #FEED5A;

`;
const Head = styled.div`
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  width: 50%;
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);

`;

export default App;
